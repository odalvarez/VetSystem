using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentsService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fix_clinicsettings_no_identity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server no permite quitar IDENTITY con AlterColumn; hay que recrear la tabla.
            // La tabla siempre está vacía en este punto (el primer insert fallaba por este mismo bug).
            migrationBuilder.Sql(@"
                CREATE TABLE [ClinicSettings_new] (
                    [Id]        int          NOT NULL,
                    [StartTime] nvarchar(5)  NOT NULL,
                    [EndTime]   nvarchar(5)  NOT NULL,
                    [WorkDays]  nvarchar(100) NOT NULL,
                    CONSTRAINT [PK_ClinicSettings] PRIMARY KEY ([Id])
                );

                IF EXISTS (SELECT 1 FROM [ClinicSettings])
                    INSERT INTO [ClinicSettings_new] ([Id],[StartTime],[EndTime],[WorkDays])
                    SELECT [Id],[StartTime],[EndTime],[WorkDays] FROM [ClinicSettings];

                DROP TABLE [ClinicSettings];
                EXEC sp_rename 'ClinicSettings_new', 'ClinicSettings';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE [ClinicSettings_old] (
                    [Id]        int          NOT NULL IDENTITY(1,1),
                    [StartTime] nvarchar(5)  NOT NULL,
                    [EndTime]   nvarchar(5)  NOT NULL,
                    [WorkDays]  nvarchar(100) NOT NULL,
                    CONSTRAINT [PK_ClinicSettings] PRIMARY KEY ([Id])
                );

                IF EXISTS (SELECT 1 FROM [ClinicSettings])
                    SET IDENTITY_INSERT [ClinicSettings_old] ON;
                    INSERT INTO [ClinicSettings_old] ([Id],[StartTime],[EndTime],[WorkDays])
                    SELECT [Id],[StartTime],[EndTime],[WorkDays] FROM [ClinicSettings];
                    SET IDENTITY_INSERT [ClinicSettings_old] OFF;

                DROP TABLE [ClinicSettings];
                EXEC sp_rename 'ClinicSettings_old', 'ClinicSettings';
            ");
        }
    }
}
