using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SirenaAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class onfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DayStats_Users_UserId",
                table: "DayStats");

            migrationBuilder.DropIndex(
                name: "IX_DayStats_UserId",
                table: "DayStats");

            migrationBuilder.CreateIndex(
                name: "IX_DayStats_UserId_Date",
                table: "DayStats",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DayStats_Users_UserId",
                table: "DayStats",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DayStats_Users_UserId",
                table: "DayStats");

            migrationBuilder.DropIndex(
                name: "IX_DayStats_UserId_Date",
                table: "DayStats");

            migrationBuilder.CreateIndex(
                name: "IX_DayStats_UserId",
                table: "DayStats",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DayStats_Users_UserId",
                table: "DayStats",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
