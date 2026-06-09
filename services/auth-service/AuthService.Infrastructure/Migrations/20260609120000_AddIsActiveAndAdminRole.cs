using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    [DbContext(typeof(AuthService.Infrastructure.Data.AuthDbContext))]
    [Migration("20260609120000_AddIsActiveAndAdminRole")]
    public partial class AddIsActiveAndAdminRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotente: la columna puede ya existir si fue agregada manualmente
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'Users') AND name = N'IsActive'
                )
                ALTER TABLE [Users] ADD [IsActive] bit NOT NULL DEFAULT 1;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsActive", table: "Users");
        }
    }
}
