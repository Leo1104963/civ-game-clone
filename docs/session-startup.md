# Session startup: starting an Agent Teams session

This is the practical how-to for starting a session for an issue in
`Leo1104963/civ-game-clone`. For the conceptual map of how agents
collaborate once the session is running, see `docs/agent-workflow.md`.

## Prerequisites

One-time setup:

1. Claude Code v2.1.32 or later.
2. Claude Max subscription.
3. `CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1` in your environment (or
   in `.claude/settings.local.json`).
4. GitHub CLI (`gh`) authenticated as `outcast1104` (handled via
   `.claude/settings.json`, no per-agent step needed).
5. `~/.claude/secrets/gh-approval-token` readable by the reviewer
   session (unchanged from before Agent Teams).

Verify:

```bash
claude --version        # >= 2.1.32
echo "$CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS"   # 1
gh auth status          # authenticated as outcast1104
test -r ~/.claude/secrets/gh-approval-token && echo "approval token OK"
```

## Start a session for an issue

Pick an issue labeled `status:ready`. Open a Claude Code chat and
paste:

```
Please run the dispatcher as the session lead for issue #<N> in
Leo1104963/civ-game-clone.
```

The session lead will:

1. Read `#<N>` and its dependencies.
2. Post `session-lead: starting on #<N>` on the issue and add
   `status:in-progress`.
3. Pick the team (feature / bug / refactor — see
   `docs/agent-workflow.md`).
4. Spin up teammates.
5. Drive the work to a merged PR, handing off to the reviewer in a
   separate session when CI is green.
6. End the session with `session-lead: done on #<N>, PR #<PR>` or
   `session-lead: needs human — <reason>`.

You do not need to start any other sessions. The lead invokes the
reviewer for you when the PR is ready.

## Re-engage after CHANGES_REQUESTED

If the reviewer returns `reviewer: verdict=CHANGES_REQUESTED`, the
session lead re-engages dev (and test-author, if the reviewer
requested test changes) in the **same** session. You do not need to
start a new session — the lead continues until the PR is merged or
it escalates with `session-lead: needs human`.

If the lead has already ended the session (e.g. it escalated and you
reviewed the issue manually), and you want to resume after fixing the
blocker, start a fresh session for the same issue number. The lead's
`session-lead: starting on #<N>` comment will appear a second time —
that is fine. Labels (`status:in-progress`) will be preserved.

## Dry-run / escalated sessions

If you want to inspect what the lead would do without actually running
the team (e.g. you are debugging a prompt change), paste:

```
Please show me what the dispatcher would do as session lead for issue
#<N> in Leo1104963/civ-game-clone, but do NOT post comments, do NOT
change labels, and do NOT invoke teammates. Just print the plan.
```

This is a manual convention — there is no dry-run flag in the
dispatcher prompt itself.

After an escalation (`session-lead: needs human`), Leonard triages
manually:

- Read the issue's `session-lead: needs human — <reason>` comment.
- Decide the next step (amend the spec, answer the design question,
  manually unblock a dependency, etc.).
- Once unblocked, start a fresh session for the same issue.

## Troubleshooting

| Symptom                                              | Likely cause                                       |
|------------------------------------------------------|---------------------------------------------------|
| Lead never posts `session-lead: starting on #<N>`    | `CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS` unset      |
| Lead posts `session-lead: blocked on #<DEP>`         | Dependency not merged — merge it, retry           |
| Reviewer session cannot read the approval token      | `~/.claude/secrets/gh-approval-token` unreadable  |
| Dev and test-author cycle on `blocked:bad-spec`      | Spec genuinely ambiguous — amend the issue        |
| No teammate called `gameplay-designer` appears       | `.claude/agents/gameplay-designer.md` missing     |

## Further reading

- `docs/agent-workflow.md` — team composition, communication
  protocol, escalation rules.
- `.claude/agents/*.md` — per-agent prompts (source of truth).
