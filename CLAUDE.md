# CLAUDE.md

Project-level instructions for all agents working on this repo.

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

## Agent rules

- **dev** works only under `src/`. Never edits `tests/`.
- **test-author** works only under `tests/`. Never edits `src/`.
- **reviewer** never writes code. Uses `gh-review-mcp` to approve or
  request changes.
- **designer** never writes code. Creates issues, maintains the backlog.
- **playtester** never writes code. Runs scenarios, reports regressions.
- All PRs target `main` and require: CI green + 1 approving review from
  `@Leo1104963` (CODEOWNERS).

## Git workflow

- One feature branch per issue: `feat/<issue-number>-<short-name>`
- Squash merge only via auto-merge
- Never push directly to `main`
- Never force-push

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
