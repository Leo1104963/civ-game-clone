# Agent workflow: Claude Code Agent Teams

This repo uses Claude Code Agent Teams
(`CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1`, Claude Code v2.1.32+, Claude
Max subscription) to run issue-driven work. The user manually starts
one session per issue. A **session lead** (the dispatcher agent) spins
up a small team of teammates to collaborate until a PR is open. The
reviewer runs in a **separate** session after the PR is green.

Source of truth for each agent's behavior is its prompt under
`.claude/agents/`. This doc is the map; the prompts are the territory.

## Team composition per task type

The session lead picks the team at startup based on the issue's labels
and title.

| Task type        | Signal                                        | Teammates                                          |
|------------------|-----------------------------------------------|----------------------------------------------------|
| **Feature**      | `type:story` (default)                        | dev, test-author, gameplay-designer                |
| **Bug**          | `type:bug`                                    | dev, test-author                                   |
| **Refactor**     | title contains "refactor"; no behavior change | dev, test-author                                   |
| **Docs / infra** | `track:docs` or `track:infra`, no `src/` edit | dev only (or manual — may not need a team at all)  |

The lead may add `gameplay-designer` to a bug team if the bug report
itself shows design ambiguity ("is this even the right behavior?").
The lead never adds the **reviewer** to the team — reviewer is a
separate session with a separate credential and is not a teammate.

## Session startup

Leonard (the user) kicks off a session for issue `<N>` from a Claude
Code chat:

```
Please run the dispatcher as the session lead for issue #<N>.
```

The lead's first actions:

1. `gh issue view <N>` — load the spec.
2. Post `session-lead: starting on #<N>` as an issue comment.
3. Add `status:in-progress`.
4. Check dependencies — if any `depends-on: #DEP` is not CLOSED,
   post `session-lead: blocked on #<DEP>`, add `status:blocked`,
   end the session.
5. Pick the team composition from the table above.
6. Spin up the teammates with the issue body as shared context via
   Agent Teams primitives. The shared task list is the primary
   intra-session state.

## Inter-agent communication protocol

Teammates talk directly via peer messages (Agent Teams primitive). A
few conventions:

- **Interface negotiation** — dev and test-author negotiate the public
  API surface before test-author commits tests. If they cannot
  converge in two rounds, either teammate signals the lead
  `<agent>: needs human — <reason>`.
- **Design questions** — dev or test-author messages
  gameplay-designer directly. Answer shape is exactly three
  sentences: the answer, the precedent (which Civ title / which
  issue), the consistency check (does this contradict any decided
  mechanic?). If gameplay-designer cannot answer from conventions
  or prior work, it says so and tells the lead
  `gameplay-designer: cannot answer from conventions; recommend
  escalation to user.`
- **Spec amendments** — if gameplay-designer believes the spec itself
  should change, it tells the lead
  `gameplay-designer: recommend spec amendment on issue #<N> —
  <one-line change>`. The lead records this; the designer (outside
  this session) picks it up. The team does not edit the issue body.
- **Blockers** — any teammate can signal the lead with
  `<agent>: needs human — <reason>` to escalate. The lead posts
  `session-lead: needs human — <reason>` on the issue and ends the
  session.

The team uses **peer messages** for coordination, not the issue
thread. The issue thread receives only the `session-lead: starting`,
`session-lead: done`, `session-lead: blocked`, and
`session-lead: needs human` comments (plus whatever the reviewer
posts on the PR). Fine-grained handoff state lives in the shared task
list, not on the issue.

## Handoff to reviewer

The reviewer is a **separate session** and is not a teammate. It uses
a distinct approval credential (`GH_APPROVAL_TOKEN`, stored at
`~/.claude/secrets/gh-approval-token`) that the authoring team does
not have. This is how the "PR author != reviewer" guarantee is
preserved architecturally.

The reviewer's behavior is defined in `.claude/agents/` — consult that
file for the full prompt. The handoff sequence is:

1. Dev pushes a PR and arms auto-merge.
2. CI runs. The lead waits until all four checks
   (build, unit-tests, game-launch-verify, lint) are green.
3. The lead invokes the reviewer agent in a new session with the PR
   number. The reviewer:
   - Checks out the PR locally.
   - Re-runs the Game Launch Verify steps.
   - Reviews against the issue's spec.
   - Posts its full review as a PR comment via `gh pr comment`.
   - Ends its session with a final log line of exactly
     `reviewer: verdict=APPROVE` or
     `reviewer: verdict=CHANGES_REQUESTED`.
4. The lead reads the verdict:
   - **APPROVE** — submit the formal approval via
     `GH_APPROVAL_TOKEN` against
     `/repos/Leo1104963/civ-game-clone/pulls/<PR>/reviews`. Auto-merge
     takes over; the lead waits for the merge.
   - **CHANGES_REQUESTED** — re-engage dev (and test-author if test
     changes are requested) in the original session. When dev
     pushes a fix, invoke the reviewer again.
5. On merge, the lead posts `session-lead: done on #<N>, PR #<PR>`,
   applies `status:done`, ends the session.

## Escalation to user

The lead escalates in exactly four cases:

1. `gameplay-designer: cannot answer from conventions; recommend
   escalation to user.` appears in the shared task list.
2. The `blocked:bad-spec` label has been set and cleared twice on the
   same issue in this session.
3. Dev reports `status:stuck` (iteration cap exhausted — 5 CI retry
   cycles).
4. Reviewer and dev cycle on CHANGES_REQUESTED three times without
   convergence.

In all four, the lead posts
`session-lead: needs human — <reason>` on the issue, leaves
`status:in-progress` in place (so Leonard can triage), and ends the
session.

## Labels the workflow uses

| Label                    | Who sets it          | Meaning                                 |
|--------------------------|----------------------|-----------------------------------------|
| `status:ready`           | Designer             | Spec is complete, dispatchable          |
| `status:in-progress`     | Session lead (start) | Session has started on this issue       |
| `status:review`          | Dev                  | PR open, waiting on reviewer            |
| `status:blocked`         | Session lead         | Dependency unmet                        |
| `status:done`            | Session lead (end)   | PR merged, story complete               |
| `status:stuck`           | Dev / test-author    | Iteration cap hit, needs human          |
| `blocked:bad-spec`       | Dev                  | Tests structurally wrong, escalating    |

## Historical note

An earlier iteration of the orchestration plan (epic #24, closed
stories #25 / #26 / #27 / #28) used a `CronCreate`-scheduled polling
dispatcher with per-handoff `gh issue comment` verbs. That direction
was abandoned 2026-04-12 in favor of Agent Teams. The closed issues
retain the rationale if you want the paper trail.
