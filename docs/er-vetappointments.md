# ER Diagram — VetAppointments

Base de datos del **appointments-service**. Cubre citas, horario global de la clínica, horarios personalizados por veterinario y ausencias.

```mermaid
erDiagram

    APPOINTMENTS {
        uniqueidentifier Id               PK
        uniqueidentifier PatientId        "snapshot patients-service"
        nvarchar_100     PatientName      "snapshot"
        uniqueidentifier OwnerId          "snapshot auth-service"
        nvarchar_200     OwnerName        "snapshot"
        nvarchar_20      OwnerPhone       "snapshot"
        nvarchar_256     OwnerEmail       "nullable, snapshot"
        uniqueidentifier VeterinarianId   "snapshot auth-service"
        nvarchar_200     VeterinarianName "snapshot"
        datetime2        ScheduledAt
        int              DurationMinutes
        nvarchar_500     Reason
        nvarchar_20      Status           "Scheduled | Completed | Cancelled | NoShow"
        nvarchar_1000    Notes            "nullable"
        bit              ReminderSent
        datetime2        CreatedAt
        datetime2        UpdatedAt
    }

    CLINICSETTINGS {
        int      Id        PK "IDENTITY, singleton"
        nvarchar StartTime "HH:mm"
        nvarchar EndTime   "HH:mm"
        nvarchar WorkDays  "ej: Mon,Tue,Wed,Thu,Fri"
    }

    VETERINARIANSCHEDULES {
        uniqueidentifier Id             PK
        uniqueidentifier VeterinarianId "snapshot auth-service"
        nvarchar_10      DayOfWeek      "Mon | Tue | Wed | Thu | Fri | Sat | Sun"
        nvarchar         StartTime      "HH:mm"
        nvarchar         EndTime        "HH:mm"
    }

    VETERINARIANLEAVES {
        uniqueidentifier Id             PK
        uniqueidentifier VeterinarianId "snapshot auth-service"
        datetime2        DateFrom
        datetime2        DateTo
        nvarchar_500     Reason
        nvarchar         StartTime      "nullable — ausencia parcial"
        nvarchar         EndTime        "nullable — ausencia parcial"
    }
```

> Las tablas `ClinicSettings`, `VeterinarianSchedules` y `VeterinarianLeaves` no tienen FK entre sí ni con `Appointments`.
> La disponibilidad se calcula cruzando las tres en tiempo real dentro del servicio.
