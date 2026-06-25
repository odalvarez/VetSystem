# notifications-service

Puerto: **5004**

Envía notificaciones a los propietarios por WhatsApp (vía Evolution API) y por correo (vía SMTP).
Guarda un registro de cada notificación enviada en su propia base de datos.

Nginx inyecta automáticamente el header `X-Internal-Key` en todas las rutas `/api/notifications/*`.
Ni el frontend ni los demás servicios necesitan enviarlo manualmente.

Base de datos propia — no comparte datos con otros servicios.

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/notifications/whatsapp` | Enviar mensaje de WhatsApp |
| POST | `/api/notifications/email` | Enviar correo electrónico |
| POST | `/api/notifications/reminder` | Programar recordatorio en tabla ReminderJobs (uso futuro) |
| GET | `/api/notifications` | Listar historial de notificaciones (paginado) |
| GET | `/api/notifications/{id}` | Consultar estado de una notificación |

## Canales soportados

- **WhatsApp** — vía Evolution API (contenedor Docker, levantado desde cero con el sistema)
- **Correo** — vía SMTP con cuenta de servicio configurada por variable de entorno

## Arquitectura de recordatorios

Los recordatorios diarios los despacha `AppointmentReminderWorker`, que corre en **appointments-service**
y llama directamente a `/api/notifications/whatsapp` y `/api/notifications/email`.
El endpoint `POST /reminder` queda disponible pero no es usado en el flujo principal.

Ver contratos completos en [CONTRATOS.md](../../CONTRATOS.md#4-notifications-service--puerto-5004).
