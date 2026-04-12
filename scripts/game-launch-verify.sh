#!/usr/bin/env bash
set -euo pipefail

# Game Launch Verify — boots the game headless and checks for errors.
# Used locally by dev agents and in CI.

GODOT="${GODOT:-godot}"
SCENE="${SCENE:-scenes/MainMenu.tscn}"
TIMEOUT="${TIMEOUT:-10}"
LOG="launch.log"

echo "=== Game Launch Verify ==="

# Step 1: headless launch
echo "Launching: $GODOT --headless --quit-after $TIMEOUT $SCENE"
$GODOT --headless --quit-after "$TIMEOUT" "$SCENE" 2>&1 | tee "$LOG" || true

# Step 2: scan log for errors
if grep -qiE "error|exception|assert|null reference" "$LOG"; then
  echo "FAIL: errors found in log:"
  grep -iE "error|exception|assert|null reference" "$LOG"
  exit 1
fi

# Step 3: check log is non-empty (process actually started)
if [ ! -s "$LOG" ]; then
  echo "FAIL: log file is empty — process may not have started"
  exit 1
fi

echo "PASS: game launched cleanly"
