# Sistema de diseño de VetSystem

Guía breve para que los cambios futuros del frontend mantengan la consistencia del rediseño "glass".
Todo el CSS está en [`frontend/wwwroot/css/app.css`](../frontend/wwwroot/css/app.css).

## Principios
1. **Un solo lugar para cada decisión.** Colores, radios, alturas y sombras salen de tokens (`:root`). No hay colores hexadecimales en `.razor` ni estilos en línea.
2. **Componentes compartidos antes que marcado repetido.** Si una pantalla necesita un modal, un badge o un icono, usa el componente; no copies el HTML.
3. **Accesible por construcción.** Cada control tiene etiqueta (`<label for>`, `aria-label`), los modales son `role="dialog"` y todo funciona con teclado.
4. **El vidrio nunca baja el contraste.** El texto sobre superficies translúcidas usa los tokens `--state-*-fg` y `--color-text*`, que pasan 4.5:1.

## Capas CSS
`@layer tokens, base, layout, components, utilities;` — la especificidad se resuelve por capa, sin `!important`
(las únicas excepciones son `prefers-reduced-motion` y `.visually-hidden`, que deben ganar siempre).

| Capa | Contenido |
|---|---|
| `tokens` | variables: color, vidrio, controles, estados, radios, tipografía |
| `base` | reseteo, fondo con manchas de color, foco visible, `.icon` |
| `layout` | `.app-layout`, sidebar flotante, barra superior móvil, cabecera de página |
| `components` | tarjetas, botones, formularios, tablas, badges, modales, etc. |
| `utilities` | `.stack`, `.row`, `.row-end`, `.text-*`, `.visually-hidden` |

## Vidrio
Solo estas superficies usan `backdrop-filter`: sidebar, barra superior, cabecera de página, tarjetas, KPIs, pestañas, modal y desplegables.
Tres salidas de emergencia mantienen la legibilidad: `@supports not (backdrop-filter…)` y `prefers-reduced-transparency` pasan a superficies
opacas, y `prefers-reduced-motion` quita animaciones.

## Componentes compartidos

| Componente | Para qué | Notas |
|---|---|---|
| `Icon` | Icono del sprite `wwwroot/icons.svg` | Siempre decorativo (`aria-hidden`); el nombre accesible lo da el texto o el `aria-label` del botón |
| `ModalShell` | Todo diálogo | Se pinta fuera del layout con `SectionContent`; pone `inert` al fondo, enfoca el diálogo y cierra con Esc. `CloseOnBackdrop` solo en vistas de consulta |
| `StatusBadge` | Cualquier estado (cita, notificación, vacuna, usuario, bitácora) | El color sale de `badge-{estado}`; un estado desconocido se ve neutro |
| `FieldError` | Mensaje bajo un campo tras el primer envío | |
| `PasswordStrength` | Requisitos visibles de contraseña | Replica `PasswordPolicy` del backend; ambos se prueban contra `tests/password-cases.json` |
| `PatientFormModal` | Alta de mascota | `ChooseOwner` activa el selector de dueño (vet/admin) |
| `PetSummary` | Ficha de solo lectura (propietario) | Encabezado + hoja de datos |

## Convenciones de marcado
- **Botones:** `type="button"` salvo el de envío de un `<form>`. Un botón solo con icono lleva `aria-label`.
- **Selección única corta** (sexo, tipo de notificación, hora de cita): `<input type="radio">` + `<label>` con clase `segmented`/`slot`.
- **Secciones plegables:** `<details class="collapse">` nativo, sin estado en C#.
- **Tablas:** `td` con `data-label`; en ≤1024 px cada fila se convierte en tarjeta.
- **Títulos:** cada página tiene un `h1` (`page-title`); las tarjetas usan `h2` enlazado con `aria-labelledby`.
- **Sin modales anidados:** un detalle dentro de un modal se muestra como vista dentro de la misma pestaña.

## Antes de dar por terminado un cambio visual
1. `dotnet test tests/VetSystem.Frontend.Tests` (componentes y paridad de contraseña).
2. `tests/VetSystem.E2E/run-in-container.sh <chromium|firefox|webkit>` (regresión funcional) y `run-in-container.sh chromium Category=Design` (accesibilidad con axe, responsive, teclado, CSP).
3. Si se añadió una clase, confirmar que se usa; si se quitó marcado, que no queden reglas huérfanas.
