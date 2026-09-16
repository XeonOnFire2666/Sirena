using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SirenaDesktop.Migrations
{
    /// <inheritdoc />
    public partial class newdaystatsmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "GitUnits",
                table: "DayStats",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "LastScoredCommitSha",
                table: "DayStats",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductivityPercent",
                table: "DayStats",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StudyUnits",
                table: "DayStats",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitUnits",
                table: "DayStats");

            migrationBuilder.DropColumn(
                name: "LastScoredCommitSha",
                table: "DayStats");

            migrationBuilder.DropColumn(
                name: "ProductivityPercent",
                table: "DayStats");

            migrationBuilder.DropColumn(
                name: "StudyUnits",
                table: "DayStats");
        }
    }
}
