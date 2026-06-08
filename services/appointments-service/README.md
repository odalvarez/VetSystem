# appointments-service

Puerto: **5003**

Gestiona la agenda del veterinario y las citas de los propietarios.
Permite crear, consultar, actualizar y cancelar citas, y verificar disponibilidad de horarios.

Base de datos propia — no comparte datos con otros servicios.

## Endpoints

| Método | Ruta | Descripción | Rol requerido |
|--------|------|-------------|---------------|
| POST | `/api/appointments` | Crear una nueva cita | cualquiera* |
| GET | `/api/appointments` | Listar citas (paginado, con filtros) | cualquiera* |
| GET | `/api/appointments/{id}` | Obtener una cita por ID | cualquiera* |
| PUT | `/api/appointments/{id}` | Actualizar fecha/motivo de una cita | cualquiera* |
| PATCH | `/api/appointments/{id}/status` | Cambiar el estado de una cita | veterinarian |
| DELETE | `/api/appointments/{id}` | Cancelar una cita | cualquiera* |
| GET | `/api/appointments/availability` | Consultar horarios disponibles | cualquiera |

\* Un `owner` solo puede gestionar las citas de sus propias mascotas.

**Estados posibles de una cita:** `scheduled` → `confirmed` → `completed` | `cancelled` | `no_show`

Ver contratos completos en [CONTRATOS.md](../../CONTRATOS.md#3-appointments-service--puerto-5003).
