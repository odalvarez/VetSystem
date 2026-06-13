using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientsService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class replace_species_slug_with_id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Elimina el índice viejo sobre el slug
            migrationBuilder.DropIndex(
                name: "IX_Patients_Species",
                table: "Patients");

            // 2. Agrega la nueva columna FK (nullable inicialmente para poder poblarla)
            migrationBuilder.AddColumn<Guid>(
                name: "SpeciesId",
                table: "Patients",
                type: "uniqueidentifier",
                nullable: true);

            // 3. Popula SpeciesId desde el slug existente en la tabla Species
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.SpeciesId = s.Id
                FROM Patients p
                INNER JOIN Species s ON s.Slug = p.Species AND s.IsDeleted = 0
            ");

            // 4. Hace la columna no nula
            migrationBuilder.AlterColumn<Guid>(
                name: "SpeciesId",
                table: "Patients",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // 5. Elimina la columna de slug
            migrationBuilder.DropColumn(
                name: "Species",
                table: "Patients");

            // 6. Crea índice sobre el nuevo FK
            migrationBuilder.CreateIndex(
                name: "IX_Patients_SpeciesId",
                table: "Patients",
                column: "SpeciesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_SpeciesId",
                table: "Patients");

            migrationBuilder.AddColumn<string>(
                name: "Species",
                table: "Patients",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE p
                SET p.Species = s.Slug
                FROM Patients p
                INNER JOIN Species s ON s.Id = p.SpeciesId AND s.IsDeleted = 0
            ");

            migrationBuilder.DropColumn(
                name: "SpeciesId",
                table: "Patients");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Species",
                table: "Patients",
                column: "Species");
        }
    }
}
