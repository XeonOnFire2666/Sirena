using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SirenaDesktop.Migrations
{
    /// <inheritdoc />
    public partial class Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PictureUrl",
                table: "User",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "StatsExistsFlag",
                columns: table => new
                {
                    DateOnly = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsExists = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatsExistsFlag", x => new { x.UserId, x.DateOnly });
                });

            migrationBuilder.CreateIndex(
                name: "IX_DayStats_UserId",
                table: "DayStats",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DayStats_User_UserId",
                table: "DayStats",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DayStats_User_UserId",
                table: "DayStats");

            migrationBuilder.DropTable(
                name: "StatsExistsFlag");

            migrationBuilder.DropIndex(
                name: "IX_DayStats_UserId",
                table: "DayStats");

            migrationBuilder.DropColumn(
                name: "PictureUrl",
                table: "User");
        }
    }
}
