# patients-service

Puerto: **5002**

Gestiona el registro de mascotas y sus historias clínicas.
Cada mascota pertenece a un `owner`. Los `veterinarian` tienen acceso a todas las mascotas.

Base de datos propia — no comparte datos con otros servicios.

## Endpoints

| Método | Ruta | Descripción | Rol requerido |
|--------|------|-------------|---------------|
| POST | `/api/patients` | Registrar una nueva mascota | owner |
| GET | `/api/patients` | Listar mascotas (paginado, con filtros) | cualquiera |
| GET | `/api/patients/{id}` | Obtener una mascota por ID | cualquiera |
| PUT | `/api/patients/{id}` | Actualizar datos de una mascota | cualquiera* |
| DELETE | `/api/patients/{id}` | Eliminar mascota e historia clínica | veterinarian |
| POST | `/api/patients/{id}/records` | Agregar entrada a la historia clínica | veterinarian |
| GET | `/api/patients/{id}/records` | Obtener historia clínica completa | cualquiera* |
| GET | `/api/patients/{id}/records/{recordId}` | Obtener una entrada específica | cualquiera* |

\* Un `owner` solo puede ver y modificar las mascotas que le pertenecen.

Ver contratos completos en [CONTRATOS.md](../../CONTRATOS.md#2-patients-service--puerto-5002).
