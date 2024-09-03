./charts/project-origin-registry/run_kind_test.sh &
(sleep 2 && ./charts/project-origin-registry/run_migration_test.sh) &
wait
