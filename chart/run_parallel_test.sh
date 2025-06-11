#!/bin/bash
current_dir="$(dirname "$0")"

"$current_dir/run_kind_test.sh" &
pid1=$!
sleep 2
"$current_dir/run_migration_test.sh" &
pid2=$!

wait $pid1
status1=$?
wait $pid2
status2=$?

if [[ $status1 -ne 0 || $status2 -ne 0 ]]; then
  echo "One or both tests failed: run_kind_test.sh exit code $status1, run_migration_test.sh exit code $status2"
  exit 1
fi
