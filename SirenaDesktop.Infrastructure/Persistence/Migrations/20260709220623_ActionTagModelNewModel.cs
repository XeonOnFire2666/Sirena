using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SirenaDesktop.Migrations
{
    /// <inheritdoc />
    public partial class ActionTagModelNewModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppTagMaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrackKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ColorHex = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTagMaps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTagMaps_UserId_TrackKey",
                table: "AppTagMaps",
                columns: new[] { "UserId", "TrackKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTagMaps");
        }
    }
}
