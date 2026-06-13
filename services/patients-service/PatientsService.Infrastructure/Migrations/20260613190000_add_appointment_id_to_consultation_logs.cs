using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PatientsService.Infrastructure.Data;

#nullable disable

namespace PatientsService.Infrastructure.Migrations
{
    [DbContext(typeof(PatientsDbContext))]
    [Migration("20260613190000_add_appointment_id_to_consultation_logs")]
    public partial class add_appointment_id_to_consultation_logs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "ConsultationLogs",
                type: "uniqueidentifier",
                nullable: true);

            // Índice único filtrado: garantiza 1 bitácora por cita sin bloquear nulos
            migrationBuilder.CreateIndex(
                name: "IX_ConsultationLogs_AppointmentId",
                table: "ConsultationLogs",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConsultationLogs_AppointmentId",
                table: "ConsultationLogs");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "ConsultationLogs");
        }
    }
}
