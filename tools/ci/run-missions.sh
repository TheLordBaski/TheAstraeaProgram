#!/bin/bash
# Runs mission autotests in the Linux player, in batch mode, and prints the result of each as a table.
# Usage: tools/ci/run-missions.sh Build/Linux/TAP.x86_64 results [missions...]
# A machine without a display needs "-nographics" (set TAP_ARGS="-nographics") or xvfb-run in front of this script.
# In a GitHub Actions job the table also goes to the job summary. Exit code 1 when a mission did not pass.
set -u
EXE=${1:-Build/Linux/TAP.x86_64}
OUT=${2:-results}
shift 2 2>/dev/null || true
MISSIONS=("$@")
[ ${#MISSIONS[@]} -eq 0 ] && MISSIONS=(flow orbit suborbital persistence docking failures escape lunar)
TIMEOUT_MIN=${TAP_TIMEOUT_MINUTES:-120}
mkdir -p "$OUT"
OUT=$(cd "$OUT" && pwd)
EXE=$(cd "$(dirname "$EXE")" && pwd)/$(basename "$EXE")
chmod +x "$EXE"

rows=()
failed=0
for m in "${MISSIONS[@]}"; do
  report="$OUT/$m.md"; log="$OUT/$m.log"
  rm -f "$report" "$log"
  echo "== $m"
  start=$(date +%s)
  timeout "${TIMEOUT_MIN}m" "$EXE" -batchmode ${TAP_ARGS:-} -autotest "$m" -report "$report" -logFile "$log"
  code=$?
  result="FAILED"; checks="-"
  if [ $code -eq 124 ]; then result="TIMEOUT"
  elif [ -f "$report" ]; then
    result=$(sed -n 's/^Result: \([A-Z]*\).*/\1/p' "$report" | head -1)
    checks=$(sed -n 's/^Checks: \([0-9]*\/[0-9]*\) passed.*/\1/p' "$report" | head -1)
    [ -z "$result" ] && result="NO RESULT"
  else result="NO REPORT"
  fi
  [ "$result" != "PASSED" ] && failed=$((failed + 1))
  mark=pass; [ "$result" != "PASSED" ] && mark=FAIL
  rows+=("| $m | $mark ($result) | ${checks:--} | $(( ($(date +%s) - start) / 60 )) min |")
  echo "   $result $checks"
done

table=$'## Missions\n\n| Mission | Result | Checks | Time |\n|---|---|---|---|'
for r in "${rows[@]}"; do table+=$'\n'"$r"; done
echo "$table"
echo "$table" > "$OUT/summary.md"
[ -n "${GITHUB_STEP_SUMMARY:-}" ] && echo "$table" >> "$GITHUB_STEP_SUMMARY"
[ $failed -gt 0 ] && exit 1
exit 0
