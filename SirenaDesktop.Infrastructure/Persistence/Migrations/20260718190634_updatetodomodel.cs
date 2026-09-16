using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SirenaDesktop.Migrations
{
    /// <inheritdoc />
    public partial class updatetodomodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "TodoItems",
                newName: "StartTime");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompleteDate",
                table: "TodoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "TodoItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompleteDate",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "TodoItems");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "TodoItems",
                newName: "DueDate");
        }
    }
}
