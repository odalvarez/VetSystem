using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientsService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsultationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ReasonForVisit = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Anamnesis = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HeartRate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RespiratoryRate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BodyCondition = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MucousMembranes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Hydration = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    TemperatureCelsius = table.Column<decimal>(type: "decimal(4,1)", nullable: true),
                    RequestedTests = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TestResults = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Prognosis = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TherapeuticPlan = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DiagnosticPlan = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Recommendations = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NextVisitDate = table.Column<DateOnly>(type: "date", nullable: true),
                    VeterinarianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VeterinarianName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultationLogs_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationLogs_PatientId",
                table: "ConsultationLogs",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationLogs_OpenedAt",
                table: "ConsultationLogs",
                column: "OpenedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ConsultationLogs");
        }
    }
}
