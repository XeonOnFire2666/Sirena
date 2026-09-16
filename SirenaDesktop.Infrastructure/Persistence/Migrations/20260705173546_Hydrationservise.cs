using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SirenaDesktop.Migrations
{
    /// <inheritdoc />
    public partial class Hydrationservise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DayStats_UserId",
                table: "DayStats");

            migrationBuilder.CreateTable(
                name: "HydratedUsers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HydratedUsers", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DayStats_UserId_Date",
                table: "DayStats",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HydratedUsers");

            migrationBuilder.DropIndex(
                name: "IX_DayStats_UserId_Date",
                table: "DayStats");

            migrationBuilder.CreateIndex(
                name: "IX_DayStats_UserId",
                table: "DayStats",
                column: "UserId");
        }
    }
}
