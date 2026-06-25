using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientsService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_vaccination_module : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VaccinationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VaccineDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VaccineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoseNumber = table.Column<int>(type: "int", nullable: false),
                    AdministeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdministeredById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdministeredByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NextDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reminder7SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reminder2SentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccinationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VaccineDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Scheme = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnnualIntervalMonths = table.Column<int>(type: "int", nullable: false, defaultValue: 12),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccineDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VaccineDoseSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VaccineDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoseNumber = table.Column<int>(type: "int", nullable: false),
                    DaysAfterPrevious = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccineDoseSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaccineDoseSteps_VaccineDefinitions_VaccineDefinitionId",
                        column: x => x.VaccineDefinitionId,
                        principalTable: "VaccineDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationRecords_NextDueDate",
                table: "VaccinationRecords",
                column: "NextDueDate");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationRecords_PatientId",
                table: "VaccinationRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationRecords_PatientId_VaccineId",
                table: "VaccinationRecords",
                columns: new[] { "PatientId", "VaccineDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_VaccineDefinitions_Name",
                table: "VaccineDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_VaccineDoseSteps_VaccineId_DoseNumber",
                table: "VaccineDoseSteps",
                columns: new[] { "VaccineDefinitionId", "DoseNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VaccinationRecords");

            migrationBuilder.DropTable(
                name: "VaccineDoseSteps");

            migrationBuilder.DropTable(
                name: "VaccineDefinitions");
        }
    }
}
