#!/usr/bin/env sh
# Corre la suite E2E dentro de una imagen basada en la oficial de Playwright para .NET, conectada a la red del stack
# aislado. Es el mismo entorno que CI: Linux, navegadores firmados por Playwright y fuentes idénticas
# (lo que hace comparables las capturas). Uso: tests/VetSystem.E2E/run-in-container.sh <navegador> [filtro]
set -eu

BROWSER="${1:-chromium}"
FILTER="${2:-Category!=Performance&Category!=Evidence}"
REPO_DIR="$(cd "$(dirname "$0")/../.." && pwd)"
IMAGE="vetsystem-e2e-runner:1.49.0"
# El runtime de .NET WebAssembly dentro del WebKit de Playwright (Linux) se cae al iniciar cuando hay muchos
# navegadores compitiendo por memoria (medido: 4,6 % de cargas fallidas con poca RAM libre, 0,4 % con holgura;
# la plantilla vacía de Blazor no falla). Con menos paralelismo el arranque deja de ser el cuello de botella
case "${BROWSER}" in webkit) E2E_PARALLEL="${E2E_PARALLEL:-4}" ;; *) E2E_PARALLEL="${E2E_PARALLEL:-0}" ;; esac

docker image inspect "${IMAGE}" >/dev/null 2>&1 || docker build -t "${IMAGE}" "${REPO_DIR}/tests/VetSystem.E2E"

# --ipc=host evita el /dev/shm de 64 MB por defecto: con varios navegadores en paralelo la memoria compartida
# se agota y WebKit deja de arrancar de forma intermitente. --init reaps los procesos huérfanos del navegador
docker run --rm --ipc=host --init \
  --network vetsystem-e2e_vetsystem \
  -v "${REPO_DIR}/tests/VetSystem.E2E:/src:ro" \
  -v "${REPO_DIR}/tests/VetSystem.E2E:/out" \
  -v "${REPO_DIR}/docs:/evidence" \
  -e E2E_BROWSER="${BROWSER}" \
  -e E2E_BASE_URL="https://frontend" \
  -e E2E_MAILPIT_URL="http://mailpit:8025" \
  -e E2E_WIREMOCK_URL="http://evolution-api:8080" \
  -e E2E_EVIDENCE_DIR="${E2E_EVIDENCE_DIR:-}" \
  -e E2E_RECORD_BASELINE="${E2E_RECORD_BASELINE:-}" \
  "${IMAGE}" \
  sh -c "cp -r /src /work && cd /work && rm -rf bin obj && dotnet test --nologo --filter '${FILTER}' -- xUnit.MaxParallelThreads=${E2E_PARALLEL}; rc=\$?
         # La línea base de rendimiento se graba en la copia de trabajo; se devuelve al repo para versionarla
         if [ -n \"\${E2E_RECORD_BASELINE}\" ] && [ -f perf-baseline.json ]; then cp perf-baseline.json /out/; fi
         exit \$rc"
