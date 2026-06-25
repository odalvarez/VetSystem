# patients-service

Puerto: **5002**

Gestiona el registro de mascotas, sus historias clínicas y las bitácoras de consulta.
Cada mascota pertenece a un `Owner`. Los `Veterinarian` y `Admin` tienen acceso a todas las mascotas.

Base de datos propia — no comparte datos con otros servicios.

## Endpoints — Mascotas

| Método | Ruta | Descripción | Rol requerido |
|--------|------|-------------|---------------|
| POST | `/api/patients` | Registrar una nueva mascota | cualquiera* |
| GET | `/api/patients` | Listar mascotas (paginado, con filtros) | cualquiera* |
| GET | `/api/patients/{id}` | Obtener una mascota por ID | cualquiera* |
| PUT | `/api/patients/{id}` | Actualizar datos de una mascota | cualquiera* |
| DELETE | `/api/patients/{id}` | Eliminar mascota e historia clínica | Veterinarian / Admin |

## Endpoints — Historia Clínica

| Método | Ruta | Descripción | Rol requerido |
|--------|------|-------------|---------------|
| POST | `/api/patients/{id}/records` | Agregar entrada a la historia clínica | Veterinarian / Admin |
| GET | `/api/patients/{id}/records` | Obtener historia clínica completa (paginado) | cualquiera* |
| GET | `/api/patients/{id}/records/{recordId}` | Obtener una entrada específica | cualquiera* |

## Endpoints — Bitácoras de Consulta

Registro en tiempo real de lo ocurrido durante una consulta. Ciclo de vida: `Open` → `Closed` (irreversible).

| Método | Ruta | Descripción | Rol requerido |
|--------|------|-------------|---------------|
| POST | `/api/patients/{patientId}/logs` | Abrir una nueva bitácora | Veterinarian / Admin |
| GET | `/api/patients/{patientId}/logs` | Listar bitácoras (paginado) | cualquiera* |
| GET | `/api/patients/{patientId}/logs/{logId}` | Obtener una bitácora específica | cualquiera* |
| PUT | `/api/patients/{patientId}/logs/{logId}` | Actualizar bitácora (solo si está Open) | Veterinarian / Admin |
| PATCH | `/api/patients/{patientId}/logs/{logId}/close` | Cerrar bitácora (irreversible) | Veterinarian / Admin |

\* Un `Owner` solo puede ver y modificar las mascotas que le pertenecen.

Ver contratos completos en [CONTRATOS.md](../../CONTRATOS.md#2-patients-service--puerto-5002).
