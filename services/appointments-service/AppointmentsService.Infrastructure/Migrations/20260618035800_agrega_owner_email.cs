using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentsService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class agrega_owner_email : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerEmail",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerEmail",
                table: "Appointments");
        }
    }
}
