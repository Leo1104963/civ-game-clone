# CLAUDE.md

Project-level instructions for all agents working on this repo.

## Agent roles

Each agent has a fixed scope. See `.claude/agents/*.md` for full per-agent
instructions. See `docs/agent-workflow.md` for how agents collaborate in
Claude Code Agent Teams sessions.

Agent Teams sessions require Claude Code v2.1.32+ with
`CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1` set in the environment (or in
`.claude/settings.local.json`). The dispatcher runs as the session
lead; dev and test-author run as teammates.
Reviewer runs in an independent session (separate credential) and is
not a teammate. See `docs/session-startup.md` for the full
prerequisites and launch checklist.

| Agent                 | Scope                                                                                 | Constraint                                                |
|-----------------------|---------------------------------------------------------------------------------------|-----------------------------------------------------------|
| **dispatcher**        | Session lead (per-story)                                                              | MUST NOT write code, reviews, issues                      |
| **designer**          | Issues, backlog, spec-phase lead in team mode, design authority during implementation | MUST NOT write code                                       |
| **dev**               | `src/` only (teammate)                                                                | MUST NOT edit `tests/`                                    |
| **test-author**       | `tests/` only (teammate)                                                              | MUST NOT edit `src/`                                      |
| **reviewer**          | PR reviews only (independent session)                                                 | MUST NOT write code; runs under a separate credential     |
| **playtester**        | Scenarios & reports                                                                   | MUST NOT write code                                       |

- All PRs target `main` and require: CI green + 1 approving review from
  `@Leo1104963` (CODEOWNERS).
- One feature branch per issue: `feat/<issue-number>-<short-name>`
- Squash merge only via auto-merge
- NEVER push directly to `main`
- NEVER force-push

## Project

Civilization-style 4X game. Godot 4.x engine, C# (.NET 8), GDScript only
for editor tools/glue.

## Commands

```bash
# Build (headless)
godot --headless --export-release "Linux/X11" build/game

# Run C# tests
dotnet test --logger "console;verbosity=detailed"

# Check formatting
dotnet format --verify-no-changes

# Game launch verify (local)
bash scripts/game-launch-verify.sh

# Lint + format fix
dotnet format
```

## Architecture

- `src/` — all game source code (C#)
- `tests/` — all unit tests (C#, xUnit/NUnit)
- `scripts/` — build and CI helper scripts
- `tools/` — infrastructure tooling (MCP servers, etc.)
- `.claude/agents/` — agent definitions
- `.github/workflows/ci.yml` — CI pipeline

## Bot identity

All agents operate as `outcast1104` automatically. Git identity and
`GH_TOKEN` are set via environment variables in `.claude/settings.json`
and `.claude/settings.local.json` — no manual setup needed per agent.

The reviewer agent additionally has access to the `Leo1104963` approval
credential via its MCP server. All other GitHub operations (commits,
pushes, PRs, issues) use the `outcast1104` bot identity.

## Code style

- C#: follow `dotnet format` defaults (Allman braces, PascalCase for
  public members, camelCase for private fields prefixed with `_`)
- Keep files focused — one class per file
- No `// TODO` without a linked GitHub issue number
- Tests use the same namespace as the code they test, suffixed with
  `.Tests`

## CI checks (all required)

1. `ci / build` — Godot headless export
2. `ci / unit-tests` — `dotnet test`
3. `ci / game-launch-verify` — headless boot + log scan
4. `ci / lint` — `dotnet format --verify-no-changes`
