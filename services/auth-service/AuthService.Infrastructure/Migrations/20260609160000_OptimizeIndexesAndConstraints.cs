using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    [DbContext(typeof(AuthService.Infrastructure.Data.AuthDbContext))]
    [Migration("20260609160000_OptimizeIndexesAndConstraints")]
    public partial class OptimizeIndexesAndConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Acotar Role a nvarchar(20) — el valor más largo es 'Veterinarian' (12 chars)
            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Acotar PasswordHash a nvarchar(72) — bcrypt produce exactamente 60 chars
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(72)",
                maxLength: 72,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // CHECK garantiza que nunca entre un rol inventado directamente en la BD
            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN ('Owner', 'Veterinarian', 'Admin')");

            // Índice compuesto para el panel admin (lista usuarios filtrando IsActive+Role)
            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive_Role",
                table: "Users",
                columns: new[] { "IsActive", "Role" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Users_IsActive_Role", table: "Users");

            migrationBuilder.DropCheckConstraint(name: "CK_Users_Role", table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(72)",
                oldMaxLength: 72);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
