current_dir="$(dirname "$0")"

"$current_dir/run_kind_test.sh" &
(sleep 2 && "$current_dir/run_migration_test.sh") &
wait
