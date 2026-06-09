#!/usr/bin/env bash
# Baja todos los contenedores sin borrar los volúmenes (datos SQL Server y sesión Evolution API)
# Para borrar también los volúmenes usar: docker compose down -v
set -e

cd "$(dirname "${BASH_SOURCE[0]}")/.."

echo "⏹  Deteniendo contenedores…"
docker compose down

echo "✅  Contenedores detenidos. Los volúmenes (datos) se conservan."
echo "    Para borrar también los datos: docker compose down -v"
