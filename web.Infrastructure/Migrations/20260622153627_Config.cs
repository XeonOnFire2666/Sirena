using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SirenaAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Config : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DayStats_Users_UserId",
                table: "DayStats");

            migrationBuilder.DropIndex(
                name: "IX_DayStats_UserId",
                table: "DayStats");
        }
    }
}
