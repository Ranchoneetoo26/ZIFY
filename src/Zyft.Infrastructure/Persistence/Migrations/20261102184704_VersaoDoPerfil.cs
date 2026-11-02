using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zyft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VersaoDoPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Profiles");
        }
    }
}
