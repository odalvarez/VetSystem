using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientsService.Infrastructure.Migrations
{
    [DbContext(typeof(PatientsService.Infrastructure.Data.PatientsDbContext))]
    [Migration("20260609160000_OptimizeIndexesAndConstraints")]
    public partial class OptimizeIndexesAndConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Acotar columnas de enum a tipos estrechos
            migrationBuilder.AlterColumn<string>(
                name: "Species",
                table: "Patients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Sex",
                table: "Patients",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // CHECK constraints: los valores inválidos no llegan a la BD
            migrationBuilder.AddCheckConstraint(
                name: "CK_Patients_Species",
                table: "Patients",
                sql: "[Species] IN ('Dog', 'Cat', 'Bird', 'Rabbit', 'Other')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Patients_Sex",
                table: "Patients",
                sql: "[Sex] IN ('Male', 'Female')");

            // Regla de negocio: el peso siempre es positivo
            migrationBuilder.AddCheckConstraint(
                name: "CK_Patients_Weight",
                table: "Patients",
                sql: "[Weight] > 0");

            // Índices para búsqueda y filtrado frecuente
            migrationBuilder.CreateIndex(
                name: "IX_Patients_Name",
                table: "Patients",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Species",
                table: "Patients",
                column: "Species");

            // Historial clínico: filtros de fecha y de veterinario son comunes
            migrationBuilder.CreateIndex(
                name: "IX_ClinicalRecords_Date",
                table: "ClinicalRecords",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalRecords_VeterinarianId",
                table: "ClinicalRecords",
                column: "VeterinarianId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_ClinicalRecords_VeterinarianId", table: "ClinicalRecords");
            migrationBuilder.DropIndex(name: "IX_ClinicalRecords_Date",           table: "ClinicalRecords");
            migrationBuilder.DropIndex(name: "IX_Patients_Species",               table: "Patients");
            migrationBuilder.DropIndex(name: "IX_Patients_Name",                  table: "Patients");

            migrationBuilder.DropCheckConstraint(name: "CK_Patients_Weight",  table: "Patients");
            migrationBuilder.DropCheckConstraint(name: "CK_Patients_Sex",     table: "Patients");
            migrationBuilder.DropCheckConstraint(name: "CK_Patients_Species", table: "Patients");

            migrationBuilder.AlterColumn<string>(
                name: "Sex",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Species",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
