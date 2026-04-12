# PROJECT_STRUCTURE.md

Living document for the Game project — agent architecture, workflow, and
enforceable quality gates.

## Vision

Solo game-dev project driven by Claude Code subagents. The human (Leonard)
sets direction; agents plan, implement, review, and playtest in parallel.
All quality gates are enforced by GitHub + CI.

**Target game:** Civilization-style 4X. Round-based strategy, 3D world map,
AI opponents, research trees, buildings, warfare, exploration, diplomacy.

**v0 scope:** smallest playable turn loop — one terrain, one unit, one
building, one tech, hotseat only, no AI.

## Core principles

1. Agents propose, tooling disposes. All merge gates are enforced by GitHub
   branch protection + required status checks.
2. Parallel by default. Independent stories run concurrently in isolated
   git worktrees.
3. Context rotation. Whoever implements does not review. Enforced by a
   second GitHub identity + MCP credential isolation.
4. Self-contained tasks. Each GitHub issue carries everything a fresh dev
   agent needs.
5. Backlog lives in GitHub. Issues + PRs + labels + branch protection.
6. Game Launch Verify is a required CI status check.

---

## Agent roster

Stored in `.claude/agents/*.md`. Frontmatter sets `model`, `effort`,
`memory`, and — for working agents — `isolation: worktree`.

| Agent | Role | Writes code? | Isolation | Concurrency |
|---|---|---|---|---|
| **designer** | Plans features, writes GH issues with self-contained specs, maintains dependency DAG, triages bugs and playtester regressions | No | None | 1 |
| **test-author** | Reads approved issue spec, writes failing unit tests on the feature branch as an executable contract for dev | Yes (tests only) | worktree | N |
| **dev** | Implements one issue on a feature branch until tests pass, runs local Game Launch Verify, opens PR. Cannot edit test files. | Yes (src only) | worktree | N |
| **reviewer** | Independent review against the issue spec, calls `pr_approve` or `pr_request_changes` via MCP | No | worktree | N |
| **playtester** | Runs scripted headless AI-vs-AI game scenarios, asserts invariants, scans logs | No | worktree | N |
| **dispatcher** | Reads backlog, computes unblocked work, fires parallel dev/reviewer/test-author agents in background, monitors running agents | No | None | 1 |

### Hard agent boundaries

- designer never writes code
- test-author writes only under `tests/` — never touches `src/`
- dev never edits files under `tests/` (enforced by CODEOWNERS)
- dev never self-reviews (enforced by branch protection + MCP isolation)
- reviewer never implements
- reviewer handles exactly ONE PR per session
- playtester never changes code

### Iteration caps

Every dev agent session has a hard limit of **5 CI retry cycles**. If the
agent cannot produce a green build + passing tests within 5 attempts:

1. Dev agent stops, adds label `status:stuck` and a comment summarizing
   what failed and why.
2. Dispatcher surfaces the stuck PR to the human.
3. No further dev agents are spawned for that issue until the human
   intervenes or designer re-specs the issue.

Test-author has a lower cap of **3 attempts** to produce a test suite that
compiles and fails for the right reasons (not import errors or typos).

---

## Workflow

```
PLAN      designer creates GH issue with self-contained spec, track label,
          depends-on links, and acceptance criteria
APPROVE   human thumbs-up the issue
SPEC      dispatcher fires test-author in a worktree. Test-author reads
          the issue spec, writes failing unit tests under tests/,
          commits to the feature branch, opens a PR.
          Tests reference types that don't exist yet — build will fail.
          EXCEPTION: `type:task` issues (infrastructure) skip this step
          and go straight to BUILD.
BUILD     dispatcher fires dev agent on the SAME branch. Dev adds
          `claimed-by:dev-<ts>` label. Dev implements under src/ until
          all tests pass, pushes to the same branch (same PR).
          Dev CANNOT edit files under tests/ (CODEOWNERS enforced).
          If stuck after 5 CI cycles → label `status:stuck`, stop.
CI        GitHub Actions runs build + unit-tests + game-launch-verify + lint
REVIEW    dispatcher fires reviewer agent once CI is fully green (all 4
          checks SUCCESS). Reviewer checks: (a) tests match spec, (b)
          impl is clean, (c) no test files touched by dev.
          KNOWN LIMITATION: MCP approval tools don't work in worktrees.
          Reviewer posts verdict as a PR comment. The main agent relays
          the formal approval via the GitHub API using the Leo1104963
          approval token.
MERGE     GitHub auto-merge fires when every protection rule is satisfied
PLAYTEST  nightly cron fires playtester agent against `main`; regressions
          open `type:bug` issues, triaged by designer (not yet implemented,
          see issue #16)
```

### Current status

Check the live state with:
```bash
gh issue list --repo Leo1104963/civ-game-clone --state all --json number,title,state,labels
```

### Stuck / bad-spec circuit breaker

If the dev agent determines that failing tests are structurally wrong (e.g.
testing a non-existent public API, contradicting the spec), it does NOT
edit the tests. Instead:

1. Dev stops, adds label `blocked:bad-spec` with a comment explaining
   the mismatch.
2. Dispatcher re-spawns test-author to revise the test suite.
3. After test-author commits a fix, dispatcher re-spawns dev.
4. If the cycle repeats twice, the issue is escalated to the human.

---

## Parallelization

### 1. Dependency DAG

Every issue carries:
- `track:<name>` label (e.g. `track:combat`, `track:ui`, `track:audio`,
  `track:save-system`)
- `depends-on: #N` lines in the body for hard prerequisites

Same-track work serializes. Cross-track work with satisfied dependencies
runs concurrently.

### 2. Per-agent worktrees

Every working agent (test-author, dev, reviewer, playtester) runs in its
own `git worktree`.

### 3. Background fan-out

Dispatcher invokes subagents with `run_in_background: true`. Ceiling:
~3–5 concurrent dev agents.

### 4. File-level collision prevention

Two issues on different tracks may still touch shared files (e.g.
`src/core/Game.cs`). Mitigations:

- Designer adds `touches:<path>` labels to issues that modify shared core
  files. Dispatcher treats `touches:` overlap as a serialization
  constraint, same as same-track.
- If two PRs conflict at merge time, the second PR's dev agent rebases
  and re-runs CI. If rebase fails, dispatcher escalates to the human.

### 5. Dispatcher monitoring loop

While agents are running, dispatcher polls at ~2-minute intervals:

1. **PR state check** — has a PR been opened? merged? failed CI?
2. **Stuck detection** — has a dev agent been running > 30 minutes with
   no new commits? Flag as potentially stuck.
3. **Stale worktree cleanup** — if an agent exited without cleaning up,
   dispatcher removes the orphan worktree.
4. **Review fan-out** — spawn reviewer only after CI is **fully green**
   (all 4 checks SUCCESS). Do NOT spawn before CI passes.
5. **Escalation** — surface `status:stuck` and `blocked:bad-spec` issues
   to the human via a summary comment on the issue.

---

## Enforceable quality gates

### Branch protection rules on `main`

Applied via `gh api repos/:owner/:repo/branches/main/protection`:

- Require status checks to pass before merge, strict (up-to-date):
  - `ci / build`
  - `ci / unit-tests`
  - `ci / game-launch-verify`
  - `ci / lint`
- Require a pull request before merge
- Require at least 1 approving review
- Dismiss stale pull request approvals when new commits are pushed
- Require review from Code Owners
- Require branches to be up to date before merging
- Require conversation resolution before merging
- Block force pushes
- Block branch deletions
- Do not allow bypass

### Context rotation

#### Credential model

- **Bot identity** (`outcast1104`, second GitHub account) — holds
  `contents:write` + `pull-requests:write`. Used by **dev**,
  **test-author**, **designer**, **playtester**, and **dispatcher** for
  all GitHub operations: commits, pushes, `gh pr create`,
  `gh issue create`, comments, labels, auto-merge.
- **Approval identity** (`Leo1104963`, primary account) — used only to
  call `pr_approve` / `pr_request_changes` / `pr_comment_review` via the
  MCP proxy. Never opens PRs, never pushes commits, never edits issues.
  Sole owner of `/` in `CODEOWNERS`.

#### MCP proxy server

`gh-review-mcp` — Python, FastMCP, ~30 lines. Owns the approval credential
and exposes:

```
pr_approve(pr_number: int, body: str)
pr_request_changes(pr_number: int, body: str)
pr_comment_review(pr_number: int, body: str)
```

Loads the approval token from `~/.claude/secrets/gh-approval-token` at
startup and invokes the GitHub REST API. The token lives only in the MCP
server process's memory.

**Scope:** exactly these three operations for v1. Extensible later.

#### Isolation

The MCP server is declared only in `reviewer.md`'s `mcpServers` frontmatter
field. No other subagent lists `gh-review-mcp`.

#### Defense-in-depth layers

1. MCP process isolation — approval credential lives in the MCP server
   process's memory
2. Per-agent MCP toolset scoping — only `reviewer.md` declares
   `gh-review-mcp` in its frontmatter
3. `CODEOWNERS` + "require review from Code Owners" — only approvals by
   `Leo1104963` count
4. "Require review from user other than PR author" — blocks self-approval
   at the API level
5. Required CI status checks — red CI blocks merge regardless of review
   state

### CI

`.github/workflows/ci.yml` runs on `pull_request` AND `push` to `main`:

```yaml
jobs:
  build:              # install Godot headless, export Linux build
  unit-tests:         # dotnet test — C# game logic suite
  game-launch-verify: # boot built binary headless, scan log for errors
  lint:               # dotnet format --verify-no-changes
  notify-failure:     # on main push failure: auto-creates type:bug issue
```

All four check jobs are required status checks. The `notify-failure` job
runs only on push to `main` and only when a check fails — it opens a
`type:bug`, `priority:critical` issue automatically so the designer can
triage it.

### Auto-merge wiring

Dev agent, immediately after `gh pr create`:

```bash
gh pr merge <N> --auto --squash
```

PR sits in auto-merge state until CI green + reviewer approval + no stale
commits + conversations resolved. GitHub merges automatically.

---

## Automation surfaces

| Layer | Tool | Trigger |
|---|---|---|
| CI on every PR | GitHub Actions | `pull_request` event |
| Auto-review on PR open | Claude Code hook in `.claude/settings.json` | PostToolUse on `gh pr create` |
| Auto-merge | `gh pr merge --auto --squash` | called by dev after `gh pr create` |
| Nightly regression | `schedule` skill / CronCreate | daily cron, 03:00 |
| Backlog dispatch loop | dispatcher agent | manual or cron |

---

## Game Launch Verify

Mandatory, run by dev before push and re-run by CI before merge:

```
1. Clean build (engine CLI)
2. Launch the built binary
3. Wait N seconds for main menu / first scene
4. Capture a screenshot
5. Scan the log for "error", "exception", "assert", "null reference"
6. Kill the process
7. PASS only if: build exit 0, process started, screenshot non-empty,
   log clean
```

Local script: `scripts/game-launch-verify.sh`. CI job: `ci / game-launch-verify`.

---

## Engine

**Engine:** Godot 4.x
**Primary language:** C# (.NET)
**Secondary language:** GDScript, optional, editor tools / glue only.

### Commands

```bash
# Headless build
godot --headless --export-release "Linux/X11" build/game

# Launch verify
godot --headless --quit-after 10 scenes/MainMenu.tscn 2>&1 | tee launch.log
grep -iE "error|exception|assert|null reference" launch.log && exit 1

# C# test suite
dotnet test --logger "console;verbosity=detailed"
```

### Test surfaces

TDD-suitable (test-author writes these as failing tests before dev starts):

- Combat resolution
- Tech tree unlocks
- Save/load roundtrip
- Turn processing
- City yields, build queues, unit movement

Not TDD-suitable (tested via Game Launch Verify + playtester instead):

- Scene loading, UI layout, rendering
- Input handling
- Audio

Higher-level integration:

- AI decision-making (invariants, no-crash on edge cases)
- Playtester runs scripted headless AI-vs-AI scenarios (e.g. 50 turns on a
  fixed-seed map, asserting no crash and no invariant violations)

### Verification layers

| Layer | Owner | When | Scope |
|---|---|---|---|
| Unit tests (TDD) | test-author → dev | Pre-merge, per issue | Pure logic: combat, yields, tech, turns, save/load |
| CI integration | GitHub Actions | Pre-merge, per PR | Build + unit-tests + lint |
| Game Launch Verify | CI + dev (local) | Pre-merge, per PR | Headless boot, log scan, screenshot |
| AI-vs-AI scenarios | playtester | Nightly, post-merge | Full-game invariants over N turns |

---

## Repo layout (target)

```
Game/
├── .claude/
│   ├── agents/
│   │   ├── designer.md
│   │   ├── test-author.md
│   │   ├── dev.md
│   │   ├── reviewer.md
│   │   ├── playtester.md
│   │   └── dispatcher.md
│   ├── agent-memory/
│   └── settings.json
├── .github/
│   ├── workflows/
│   │   └── ci.yml
│   └── CODEOWNERS
├── scripts/
│   ├── game-launch-verify.sh
│   └── dispatch.sh
├── src/
├── tests/
├── CLAUDE.md
└── PROJECT_STRUCTURE.md
```

---

## Resolved questions

1. **Engine** — Godot 4 + C#
2. **Context rotation** — MCP proxy (Python) + two GitHub identities
3. **Agent Teams primitive** — evaluated, declined. GitHub Issues as
   durable task queue instead.
4. **GitHub repo** — `Leo1104963/civ-game-clone`
5. **Bot account** — `outcast1104`, write collaborator

## Known limitations

1. **MCP approval in worktrees doesn't work.** The reviewer agent's
   `mcpServers` frontmatter doesn't resolve when running in a worktree
   via the `Agent` tool. Workaround: reviewer posts verdict as a PR
   comment, the main agent relays the formal approval via:
   ```bash
   GH_TOKEN=$(cat ~/.claude/secrets/gh-approval-token) \
     gh api repos/Leo1104963/civ-game-clone/pulls/N/reviews \
     -X POST -f event="APPROVE" -f body="..."
   ```

2. **Branch protection requires manual setup.** Neither the bot token
   nor the approval token has `administration` scope. Branch protection
   must be configured from the GitHub web UI by `Leo1104963`.

3. **`test-author` subagent type not registered at runtime.** Use
   general-purpose agents with test-author instructions instead. The
   `dev`, `reviewer`, `designer`, `dispatcher`, `playtester` types
   are registered and work.

4. **Nightly playtester not implemented.** See issue #16. Currently
   no automated regression testing against `main` outside of CI.

## Infrastructure setup (completed)

All items below are done:

- [x] Bot account `outcast1104` with write access
- [x] Approval token at `~/.claude/secrets/gh-approval-token`
- [x] Bot token at `~/.claude/secrets/gh-bot-token`
- [x] `gh-review-mcp` server at `tools/gh-review-mcp/server.py`
- [x] `CLAUDE.md` with commands, architecture, agent rules
- [x] Six agent files in `.claude/agents/`
- [x] `.github/workflows/ci.yml` with 4 check jobs + notify-failure
- [x] `.github/CODEOWNERS` — `* @Leo1104963`, `tests/** @Leo1104963`
- [x] Branch protection on `main` (via GitHub UI)
- [x] `scripts/game-launch-verify.sh`
- [x] Bot identity implicit via env vars in `settings.json` /
      `settings.local.json`
- [x] `CivGame.sln` with `src/CivGame/` + `tests/CivGame.Tests/`
- [x] Godot 4.4 project init (`project.godot`, scenes, export presets)
- [x] GitHub labels for tracks, status, types
- [x] Auto-merge + auto-delete head branches enabled

## Agent Teams orchestration

This repo uses Claude Code Agent Teams for issue-driven work.

- **Env var**: `CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1`
- **Minimum Claude Code version**: v2.1.32
- **Subscription**: Claude Max

The session lead (dispatcher agent) is invoked manually per-issue by
the user. Teammates (dev, test-author, gameplay-designer) collaborate
directly via peer messages. The reviewer runs in a separate session.

See:
- `docs/agent-workflow.md` — team composition, communication
  protocol, escalation rules.
- `docs/session-startup.md` — how to start a session from a chat.
- `.claude/agents/*.md` — per-agent prompts (source of truth).
