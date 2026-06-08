# auth-service

Puerto: **5001**

Gestiona el registro de usuarios, la autenticación y los roles del sistema.
Emite tokens JWT que los demás servicios validan para controlar el acceso.

Roles disponibles: `veterinarian` | `owner`.

## Endpoints

| Método | Ruta | Descripción | Público |
|--------|------|-------------|---------|
| POST | `/api/auth/register` | Registrar un nuevo usuario | Sí |
| POST | `/api/auth/login` | Autenticar y obtener JWT | Sí |
| GET | `/api/auth/me` | Obtener perfil del usuario autenticado | No |
| PUT | `/api/auth/me` | Actualizar nombre y teléfono | No |
| POST | `/api/auth/change-password` | Cambiar contraseña | No |

Ver contratos completos (modelos de request/response y códigos HTTP) en
[CONTRATOS.md](../../CONTRATOS.md#1-auth-service--puerto-5001).
