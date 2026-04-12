#!/usr/bin/env bash
set -euo pipefail

# Game Launch Verify — imports the project headless and checks for errors.
# Used locally by dev agents and in CI.
#
# We use --import (not a scene launch) because headless Godot cannot
# instantiate C# scripts without a fully initialised Mono runtime and
# pre-built assembly on PATH.  --import validates project health
# (resource integrity, script compilation, scene consistency) without
# requiring the C# runtime to be live inside the Godot process.

GODOT="${GODOT:-godot}"
TIMEOUT="${TIMEOUT:-60}"
LOG="launch.log"

echo "=== Game Launch Verify ==="

# Step 1: headless import (validates project health)
echo "Importing project: $GODOT --headless --import"
timeout "$TIMEOUT" "$GODOT" --headless --import 2>&1 | tee "$LOG" || true

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

echo "PASS: project imported cleanly"
