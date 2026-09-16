using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SirenaDesktop.Migrations
{
    /// <inheritdoc />
    public partial class WSmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Todos",
                table: "Todos");

            migrationBuilder.RenameTable(
                name: "Todos",
                newName: "WorkspaceTodos");

            migrationBuilder.RenameColumn(
                name: "TodoId",
                table: "PendingChanges",
                newName: "EntityId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "WorkspaceTodos",
                newName: "WorkspaceId");

            migrationBuilder.AddColumn<int>(
                name: "EntityKind",
                table: "PendingChanges",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkspaceTodos",
                table: "WorkspaceTodos",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspaceTypeId = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceTodos_WorkspaceId",
                table: "WorkspaceTodos",
                column: "WorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceTodos_Workspaces_WorkspaceId",
                table: "WorkspaceTodos",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceTodos_Workspaces_WorkspaceId",
                table: "WorkspaceTodos");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkspaceTodos",
                table: "WorkspaceTodos");

            migrationBuilder.DropIndex(
                name: "IX_WorkspaceTodos_WorkspaceId",
                table: "WorkspaceTodos");

            migrationBuilder.DropColumn(
                name: "EntityKind",
                table: "PendingChanges");

            migrationBuilder.RenameTable(
                name: "WorkspaceTodos",
                newName: "Todos");

            migrationBuilder.RenameColumn(
                name: "EntityId",
                table: "PendingChanges",
                newName: "TodoId");

            migrationBuilder.RenameColumn(
                name: "WorkspaceId",
                table: "Todos",
                newName: "UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Todos",
                table: "Todos",
                column: "Id");
        }
    }
}
