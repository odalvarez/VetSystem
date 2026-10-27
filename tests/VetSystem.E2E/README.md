# Pruebas E2E de VetSystem

Suite de regresión de punta a punta con Playwright. Corre contra un **stack Docker aislado**
(`-p vetsystem-e2e`) que no comparte contenedores, red ni volúmenes con el stack de desarrollo.

Cómo está armado el stack:
- **Frontend:** usa la configuración de nginx de producción (`frontend/nginx/default.conf`), con TLS y un certificado autofirmado que se genera en cada corrida.
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

El camino recomendado (y el de CI) es el contenedor: Linux, navegadores de Playwright con versión fija y las mismas fuentes que en CI. Requiere el stack levantado.

```bash
MSYS_NO_PATHCONV=1 sh tests/VetSystem.E2E/run-in-container.sh chromium
```

El primer argumento es el navegador (`chromium`, `firefox` o `webkit`) y el segundo, un filtro opcional de xUnit. Por defecto excluye `Performance` y `Evidence`. `E2E_PARALLEL` limita los hilos de xUnit (en WebKit son 4 por defecto, ver más abajo).

Sin contenedor, contra el stack local, `E2E_BROWSER` elige el motor:

```bash
E2E_BROWSER=chromium dotnet test tests/VetSystem.E2E --filter "Category!=Performance&Category!=Evidence"
```

### WebKit

El WebKit de Playwright en Linux no es Safari: el runtime de .NET WebAssembly se cae al iniciar de forma intermitente (`Out of bounds memory access`, `garbage appeared in memory at address 0`). Medido: 0 de 240 cargas con la plantilla vacía de Blazor y entre 0,4 % y 4,6 % con esta app, según la memoria libre del equipo; sin nginx no se reprodujo en 120 cargas. Hay un antecedente en WebKit con el mismo síntoma al correr varias instancias a la vez ([bug 286266](https://bugs.webkit.org/show_bug.cgi?id=286266)). Por eso en CI es informativo y no bloquea, y el arranque fallido se reporta con el contenido de `#app` y los eventos del navegador en lugar de un simple tiempo de espera.

### Categorías

| Categoría | Qué hace | Cuándo |
|---|---|---|
| (sin categoría) | Regresión funcional por rol y enrutamiento de la API | Siempre, en los 3 navegadores |
| `Design` | Accesibilidad (axe) en 16 pantallas, desbordes, modales, CSP, fuente Inter y arranque en frío | Siempre, en los 3 navegadores |
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
