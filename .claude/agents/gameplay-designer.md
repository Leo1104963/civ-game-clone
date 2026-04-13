---
name: gameplay-designer
description: RETIRED. Role merged into the designer agent on 2026-04-13. Do not invoke.
model: sonnet
memory: project
---

# Gameplay Designer Agent (RETIRED)

This agent was retired on **2026-04-13**. The dispatcher no longer
spins it up, and no other agent should invoke it.

## Why it was retired

The earlier workflow had the dispatcher spin up `dev + test-author +
gameplay-designer`, and the designer wrote issues in a separate solo
session. That created a gap: the gameplay-designer had to **guess** at
what the issue's author intended whenever the spec was ambiguous,
because the original designer was not in the session.

The new workflow (see `.claude/agents/dispatcher.md` and
`.claude/agents/designer.md`) folds the design-authority role into the
designer itself. The dispatcher now spins up `designer + dev +
test-author` as a trio. The designer leads a collaborative spec-writing
phase (with dev and test-author flagging concerns live), files the
finalized story for traceability, then stays in the session as the
final authority on design questions throughout implementation.

## What replaces it

- **Spec writing**: still the designer (now collaborative, in-session).
- **Design-intent answers during implementation**: the designer (no
  guessing — the original author is present).
- **Spec amendments mid-implementation**: the designer amends the
  issue body in place during the session.

If you find a reference to `gameplay-designer` in a spec, comment, or
historical document, treat it as referring to the designer in team
mode.

## Do not invoke

This file is preserved only so historical references resolve. Do not
read its previous contents for behavioral guidance — read
`.claude/agents/designer.md` "Team mode" section instead.
