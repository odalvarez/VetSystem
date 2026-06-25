# ER Diagram — VetNotifications

Base de datos del **notifications-service**. Registra todos los envíos realizados y la cola de recordatorios de citas.

```mermaid
erDiagram

    NOTIFICATIONS {
        uniqueidentifier Id        PK
        nvarchar         Type      "WhatsApp | Email"
        nvarchar_256     Recipient "teléfono o dirección email"
        nvarchar_200     Subject   "nullable — solo correos"
        nvarchar_max     Body
        nvarchar_450     Status    "Pending | Sent | Failed"
        datetime2        SentAt    "nullable"
        nvarchar_max     Error     "nullable"
        datetime2        CreatedAt
    }

    REMINDERS {
        uniqueidentifier Id              PK
        uniqueidentifier AppointmentId   "snapshot appointments-service"
        nvarchar_100     PatientName     "snapshot"
        nvarchar_200     OwnerName       "snapshot"
        nvarchar_20      OwnerPhone      "snapshot"
        nvarchar_256     OwnerEmail      "nullable, snapshot"
        datetime2        AppointmentAt
        datetime2        ScheduledSendAt
        nvarchar_50      Channels        "WhatsApp | Email | Both"
        bit              Sent
        datetime2        CreatedAt
    }
```

> `Notifications` y `Reminders` no tienen FK entre sí.
> Los recordatorios de vacunación se procesan directamente desde el `VaccinationReminderWorker`
> en patients-service y no pasan por esta base de datos.
