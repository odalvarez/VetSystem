#!/usr/bin/env bash
# Levanta todo el sistema desde la raíz del proyecto
# Uso: ./docker/up.sh
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$SCRIPT_DIR/.."

cd "$ROOT"

if [ ! -f .env ]; then
  echo "ERROR: no se encontró el archivo .env en la raíz del proyecto."
  echo "Copia .env.example a .env y rellena los valores reales."
  exit 1
fi

echo "▶  Construyendo imágenes y levantando contenedores…"
docker compose up --build -d

echo ""
echo "▶  Esperando a que todos los servicios estén healthy…"
docker compose wait sqlserver auth-service patients-service appointments-service notifications-service frontend 2>/dev/null || true

echo ""
echo "✅  Sistema levantado. Estado actual:"
docker compose ps
