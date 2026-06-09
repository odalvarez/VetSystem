#!/usr/bin/env bash
# Muestra el estado de todos los contenedores y sus healthchecks
cd "$(dirname "${BASH_SOURCE[0]}")/.."

echo "=== Contenedores ==="
docker compose ps

echo ""
echo "=== Health checks (últimos 5 eventos) ==="
for svc in sqlserver evolution-api auth-service patients-service appointments-service notifications-service frontend; do
  state=$(docker inspect --format='{{.State.Health.Status}}' "vetsystem-$svc" 2>/dev/null || echo "not found")
  printf "  %-30s %s\n" "$svc" "$state"
done
