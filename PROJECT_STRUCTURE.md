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
| **designer** | Plans features, writes GH issues with self-contained specs, maintains dependency DAG, triages bugs | No | None | 1 |
| **dev** | Implements one issue on a feature branch, runs local Game Launch Verify, opens PR | Yes | worktree | N |
| **reviewer** | Independent review against the issue spec, calls `pr_approve` or `pr_request_changes` via MCP | No | worktree | N |
| **playtester** | Runs scripted headless AI-vs-AI game scenarios, asserts invariants, scans logs | No | worktree | N |
| **dispatcher** | Reads backlog, computes unblocked work, fires parallel dev/reviewer agents in background | No | None | 1 |

### Hard agent boundaries

- designer never writes code
- dev never self-reviews (enforced by branch protection + MCP isolation)
- reviewer never implements
- reviewer handles exactly ONE PR per session
- playtester never changes code

---

## Workflow

```
PLAN      designer creates GH issue with self-contained spec, track label,
          depends-on links
APPROVE   human thumbs-up the issue
CLAIM     dispatcher fires a dev agent in a worktree; dev adds
          `claimed-by:dev-<ts>` label
BUILD     dev implements, runs local Game Launch Verify, commits, opens
          PR via `gh pr create`, then `gh pr merge --auto --squash`
CI        GitHub Actions runs build + unit-tests + game-launch-verify + lint
REVIEW    dispatcher (or post-PR-open hook) spawns reviewer agent in fresh
          worktree. Reviewer calls `pr_approve` or `pr_request_changes`
          via gh-review-mcp
MERGE     GitHub auto-merge fires when every protection rule is satisfied
PLAYTEST  nightly cron fires playtester agent against `main`; regressions
          open `type:bug` issues
```

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

Every working agent (dev, reviewer, playtester) runs in its own
`git worktree`.

### 3. Background fan-out

Dispatcher invokes subagents with `run_in_background: true`. Ceiling:
~3–5 concurrent dev agents.

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

- **Bot identity** (`leonard-dev-bot`, second GitHub account) — holds
  `contents:write` + `pull-requests:write`. Used by **dev**, **designer**,
  **playtester**, and **dispatcher** for all GitHub operations: commits,
  pushes, `gh pr create`, `gh issue create`, comments, labels, auto-merge.
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

`.github/workflows/ci.yml` runs on every `pull_request`:

```yaml
jobs:
  build:              # install Godot headless, export Linux build
  unit-tests:         # dotnet test — C# game logic suite
  game-launch-verify: # boot built binary headless, scan log for errors
  lint:               # dotnet format --verify-no-changes
```

All four are required status checks.

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

- Combat resolution
- AI decision-making (invariants, no-crash on edge cases)
- Tech tree unlocks
- Save/load roundtrip
- Turn processing
- City yields, build queues, unit movement

Playtester runs scripted headless AI-vs-AI scenarios (e.g. 50 turns on a
fixed-seed map, asserting no crash and no invariant violations).

---

## Repo layout (target)

```
Game/
├── .claude/
│   ├── agents/
│   │   ├── designer.md
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
├── CLAUDE.md
└── PROJECT_STRUCTURE.md
```

---

## Open questions

1. ~~Engine~~ — **RESOLVED: Godot 4 + C#**
2. ~~Context rotation~~ — **RESOLVED: MCP proxy (Python) + two GitHub
   identities**
3. **GitHub repo** — name + `gh repo create`
4. **Bot account** — `leonard-dev-bot` needs to be created, 2FA enabled,
   PAT generated, added as collaborator

## Next steps

1. Create `leonard-dev-bot` GitHub account, enable 2FA, generate PAT
   with `contents:write` + `pull-requests:write`
2. `gh repo create`, initial commit, add bot account as collaborator
3. Store approval token at `~/.claude/secrets/gh-approval-token`
4. Build `gh-review-mcp` — Python + FastMCP, three tools
5. Draft `CLAUDE.md` with engine-specific commands
6. Draft five agent files. `reviewer.md` declares `gh-review-mcp` in
   `mcpServers`
7. Draft `.github/workflows/ci.yml` — `build`, `unit-tests`,
   `game-launch-verify`, `lint`
8. Draft `CODEOWNERS` — `* @Leo1104963`
9. Apply branch protection via `gh api ... /branches/main/protection`
10. Stub `scripts/game-launch-verify.sh`
11. Stub `.claude/settings.json` hooks for auto-review-on-PR and
    auto-dispatch
