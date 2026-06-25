# auth-service

Puerto: **5001**

Gestiona el registro de usuarios, la autenticación y los roles del sistema.
Emite tokens JWT que los demás servicios validan para controlar el acceso.

Roles disponibles: `Admin` | `Veterinarian` | `Owner`.

## Endpoints

| Método | Ruta | Descripción | Público |
|--------|------|-------------|---------|
| POST | `/api/auth/register` | Registrar un nuevo usuario | Sí |
| POST | `/api/auth/login` | Autenticar y obtener JWT en cookie httpOnly | Sí |
| POST | `/api/auth/logout` | Eliminar la cookie de sesión | Sí |
| GET | `/api/auth/me` | Obtener perfil del usuario autenticado | No |
| PUT | `/api/auth/me` | Actualizar nombre y teléfono | No |
| POST | `/api/auth/change-password` | Cambiar contraseña | No |
| GET | `/api/auth/owners` | Listar propietarios (para selects) | Veterinarian / Admin |
| GET | `/api/auth/veterinarians` | Listar veterinarios (para selects) | Todos los roles |

### Administración (solo Admin)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/auth/admin/users` | Listar todos los usuarios (paginado, con filtros) |
| GET | `/api/auth/admin/users/{id}` | Obtener usuario por ID |
| POST | `/api/auth/admin/users` | Crear usuario desde el panel de administración |
| PUT | `/api/auth/admin/users/{id}` | Actualizar nombre, teléfono y rol |
| PATCH | `/api/auth/admin/users/{id}/active` | Activar o desactivar cuenta |
| POST | `/api/auth/admin/users/{id}/reset-password` | Restablecer contraseña sin la actual |

Ver contratos completos (modelos de request/response y códigos HTTP) en
[CONTRATOS.md](../../CONTRATOS.md#1-auth-service--puerto-5001).
