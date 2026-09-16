using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SirenaDesktop.Migrations
{
    /// <inheritdoc />
    public partial class addlistofSHAsdaystats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastScoredCommitSha",
                table: "DayStats");

            migrationBuilder.AddColumn<string>(
                name: "ScoredCommitShas",
                table: "DayStats",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScoredCommitShas",
                table: "DayStats");

            migrationBuilder.AddColumn<string>(
                name: "LastScoredCommitSha",
                table: "DayStats",
                type: "TEXT",
                nullable: true);
        }
    }
}
