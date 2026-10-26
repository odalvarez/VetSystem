# Pruebas E2E de VetSystem

Suite de regresión de punta a punta con Playwright. Corre contra un **stack Docker aislado**
(`-p vetsystem-e2e`) que no comparte contenedores, red ni volúmenes con el stack de desarrollo.

Cómo está armado el stack:
- **Frontend:** usa el `nginx.conf` real de producción, con TLS y un certificado autofirmado que se genera en cada corrida.
- **Correo:** va a **Mailpit**, que guarda los mensajes en lugar de entregarlos.
- **WhatsApp:** responde **WireMock** como si fuera Evolution API. Los números que terminan en `3009999999` reciben un 500, para simular un fallo de envío.
- **Datos:** salen de `e2e.env`. Son valores solo de prueba y no se usa el `.env` real.

## Levantar y bajar el stack

```bash
docker compose -p vetsystem-e2e --env-file tests/VetSystem.E2E/e2e.env -f docker-compose.yml -f docker-compose.e2e.yml up -d --build --wait
```

```bash
docker compose -p vetsystem-e2e -f docker-compose.yml -f docker-compose.e2e.yml down -v
```

`down -v` solo borra los volúmenes del proyecto `vetsystem-e2e`.

## Instalar los navegadores (una vez)

```bash
dotnet build tests/VetSystem.E2E
```

```bash
tests/VetSystem.E2E/bin/Debug/net9.0/.playwright/node/win32_x64/node.exe tests/VetSystem.E2E/bin/Debug/net9.0/.playwright/package/cli.js install chromium firefox webkit
```

En Linux/CI, reemplazar la ruta de `node` por `linux_x64`.

## Correr la suite

`E2E_BROWSER` elige el motor: `chromium` (por defecto), `firefox` o `webkit`.

```bash
E2E_BROWSER=chromium dotnet test tests/VetSystem.E2E --filter "Category!=Performance&Category!=Evidence"
```

### Categorías

| Categoría | Qué hace | Cuándo |
|---|---|---|
| (sin categoría) | Regresión funcional por rol y enrutamiento de la API | Siempre, en los 3 navegadores |
| `Performance` | Presupuesto de rendimiento contra `perf-baseline.json` (mediana de 5 cargas en frío, tolerancia 10%) | Solo Chromium |
| `Evidence` | Capturas de cada pantalla a 1440px y 390px en `E2E_EVIDENCE_DIR` | A pedido |

Para grabar una nueva línea base de rendimiento, que solo debe hacerse con un cambio de alcance aprobado:

```bash
E2E_RECORD_BASELINE=1 dotnet test tests/VetSystem.E2E --filter "Category=Performance"
```

## Principios

- **Selectores semánticos.** Los tests usan `GetByRole`, `GetByLabel` y `GetByText`, nunca clases CSS. Así la misma suite valida la UI antes y después de un rediseño.
- **Datos propios.** Cada test crea los datos que necesita por API; el orden de ejecución no importa.
- **Sin esperas fijas.** Las aserciones de Playwright esperan por la condición, y los envíos asíncronos se consultan hasta un plazo máximo.
