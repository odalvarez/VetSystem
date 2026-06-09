using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentsService.Infrastructure.Migrations
{
    [DbContext(typeof(AppointmentsService.Infrastructure.Data.AppointmentsDbContext))]
    [Migration("20260609160000_OptimizeIndexesAndConstraints")]
    public partial class OptimizeIndexesAndConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Acotar Status a nvarchar(20) — el valor más largo es 'Scheduled' (9 chars)
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Appointments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // CHECK bloquea estados fuera del ciclo de vida permitido a nivel de BD
            migrationBuilder.AddCheckConstraint(
                name: "CK_Appointments_Status",
                table: "Appointments",
                sql: "[Status] IN ('Scheduled', 'Confirmed', 'Completed', 'Cancelled', 'NoShow')");

            // La regla de negocio de duración queda anclada también en la BD
            migrationBuilder.AddCheckConstraint(
                name: "CK_Appointments_Duration",
                table: "Appointments",
                sql: "[DurationMinutes] >= 10 AND [DurationMinutes] <= 480");

            // PatientId sin índice causaría full-scan al consultar citas de una mascota
            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

            // Filtros de agenda por rango de fechas (sin restringir a un veterinario)
            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScheduledAt",
                table: "Appointments",
                column: "ScheduledAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Appointments_ScheduledAt", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_Appointments_PatientId",   table: "Appointments");

            migrationBuilder.DropCheckConstraint(name: "CK_Appointments_Duration", table: "Appointments");
            migrationBuilder.DropCheckConstraint(name: "CK_Appointments_Status",   table: "Appointments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
