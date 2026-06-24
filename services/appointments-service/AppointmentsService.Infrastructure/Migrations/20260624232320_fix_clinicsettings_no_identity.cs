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
            // SQL Server no permite ni AlterColumn para quitar IDENTITY ni reusar nombres de
            // constraints dentro de la misma transacción. suppressTransaction:true hace que
            // cada comando se commitee de forma independiente, liberando el catálogo entre pasos.

            migrationBuilder.Sql(
                "IF OBJECT_ID('ClinicSettings_new','U') IS NOT NULL DROP TABLE [ClinicSettings_new];",
                suppressTransaction: true);

            migrationBuilder.Sql(@"
                CREATE TABLE [ClinicSettings_new] (
                    [Id]        int           NOT NULL,
                    [StartTime] nvarchar(5)   NOT NULL,
                    [EndTime]   nvarchar(5)   NOT NULL,
                    [WorkDays]  nvarchar(100) NOT NULL
                );",
                suppressTransaction: true);

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClinicSettings')
                AND EXISTS (SELECT 1 FROM [ClinicSettings])
                    INSERT INTO [ClinicSettings_new] ([Id],[StartTime],[EndTime],[WorkDays])
                    SELECT [Id],[StartTime],[EndTime],[WorkDays] FROM [ClinicSettings];",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "IF OBJECT_ID('ClinicSettings','U') IS NOT NULL DROP TABLE [ClinicSettings];",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "EXEC sp_rename 'ClinicSettings_new', 'ClinicSettings';",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "ALTER TABLE [ClinicSettings] ADD CONSTRAINT [PK_ClinicSettings] PRIMARY KEY ([Id]);",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF OBJECT_ID('ClinicSettings_old','U') IS NOT NULL DROP TABLE [ClinicSettings_old];",
                suppressTransaction: true);

            migrationBuilder.Sql(@"
                CREATE TABLE [ClinicSettings_old] (
                    [Id]        int           NOT NULL IDENTITY(1,1),
                    [StartTime] nvarchar(5)   NOT NULL,
                    [EndTime]   nvarchar(5)   NOT NULL,
                    [WorkDays]  nvarchar(100) NOT NULL,
                    CONSTRAINT [PK_ClinicSettings] PRIMARY KEY ([Id])
                );",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "IF OBJECT_ID('ClinicSettings','U') IS NOT NULL DROP TABLE [ClinicSettings];",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "EXEC sp_rename 'ClinicSettings_old', 'ClinicSettings';",
                suppressTransaction: true);
        }
    }
}
