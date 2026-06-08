# notifications-service

Puerto: **5004**

Envía notificaciones a los propietarios por WhatsApp (vía Evolution API) y por correo (vía SMTP).
También programa recordatorios automáticos 24 horas antes de cada cita.

**Servicio interno.** El frontend no lo llama directamente.
Solo los demás microservicios lo invocan. Todas las solicitudes deben incluir
el header `X-Internal-Key` además del JWT.

Base de datos propia — no comparte datos con otros servicios.

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/notifications/whatsapp` | Enviar mensaje de WhatsApp |
| POST | `/api/notifications/email` | Enviar correo electrónico |
| POST | `/api/notifications/reminder` | Programar recordatorio de cita (24h antes) |
| GET | `/api/notifications/{id}` | Consultar estado de una notificación |

## Canales soportados

- **WhatsApp** — vía Evolution API (contenedor Docker, levantado desde cero con el sistema)
- **Correo** — vía SMTP con cuenta de servicio configurada por variable de entorno

Ver contratos completos en [CONTRATOS.md](../../CONTRATOS.md#4-notifications-service--puerto-5004).
