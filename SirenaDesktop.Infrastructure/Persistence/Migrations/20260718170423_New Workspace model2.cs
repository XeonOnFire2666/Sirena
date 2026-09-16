using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SirenaDesktop.Migrations
{
    /// <inheritdoc />
    public partial class NewWorkspacemodel2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "Workspaces",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "Workspaces");
        }
    }
}
