using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SirenaAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class @new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Todos",
                table: "Todos");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Todos");

            migrationBuilder.RenameTable(
                name: "Todos",
                newName: "TodoItem");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceItemId",
                table: "TodoItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TodoItem",
                table: "TodoItem",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceTypeId = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItem_WorkspaceItemId",
                table: "TodoItem",
                column: "WorkspaceItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItem_Workspaces_WorkspaceItemId",
                table: "TodoItem",
                column: "WorkspaceItemId",
                principalTable: "Workspaces",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoItem_Workspaces_WorkspaceItemId",
                table: "TodoItem");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TodoItem",
                table: "TodoItem");

            migrationBuilder.DropIndex(
                name: "IX_TodoItem_WorkspaceItemId",
                table: "TodoItem");

            migrationBuilder.DropColumn(
                name: "WorkspaceItemId",
                table: "TodoItem");

            migrationBuilder.RenameTable(
                name: "TodoItem",
                newName: "Todos");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Todos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Todos",
                table: "Todos",
                column: "Id");
        }
    }
}
