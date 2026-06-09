using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientsService.Infrastructure.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(
        typeof(PatientsService.Infrastructure.Data.PatientsDbContext))]
    [Migration("20260609180000_AddSpeciesTable")]
    public partial class AddSpeciesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Quitar constraint estático: especies serán administradas desde BD
            migrationBuilder.DropCheckConstraint(
                name: "CK_Patients_Species",
                table: "Patients");

            // 2. Ampliar columna para slugs de nombres personalizados (max 50)
            migrationBuilder.AlterColumn<string>(
                name: "Species",
                table: "Patients",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            // 3. Normalizar datos históricos: "Dog"→"dog", "Cat"→"cat", etc.
            migrationBuilder.Sql("UPDATE Patients SET Species = LOWER(Species)");

            // 4. Crear tabla de especies administradas
            migrationBuilder.CreateTable(
                name: "Species",
                columns: t => new
                {
                    Id        = t.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name      = t.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug      = t.Column<string>(type: "nvarchar(50)",  maxLength: 50,  nullable: false),
                    IsActive  = t.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = t.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_Species", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Species_Slug",
                table: "Species",
                column: "Slug",
                unique: true);

            // 5. Sembrar las 5 especies iniciales que ya existían como enum
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            migrationBuilder.Sql($@"
                INSERT INTO Species (Id, Name, Slug, IsActive, CreatedAt) VALUES
                (NEWID(), 'Perro',  'dog',    1, '{now}'),
                (NEWID(), 'Gato',   'cat',    1, '{now}'),
                (NEWID(), 'Ave',    'bird',   1, '{now}'),
                (NEWID(), 'Conejo', 'rabbit', 1, '{now}'),
                (NEWID(), 'Otro',   'other',  1, '{now}')
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Species");

            migrationBuilder.AlterColumn<string>(
                name: "Species",
                table: "Patients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Patients_Species",
                table: "Patients",
                sql: "[Species] IN ('Dog', 'Cat', 'Bird', 'Rabbit', 'Other')");
        }
    }
}
