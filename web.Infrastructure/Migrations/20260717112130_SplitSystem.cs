using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SirenaAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoItem_Workspaces_WorkspaceItemId",
                table: "TodoItem");

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
                newName: "TodoItems");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "TodoItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_TodoItems",
                table: "TodoItems",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_WorkspaceId",
                table: "TodoItems",
                column: "WorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_Workspaces_WorkspaceId",
                table: "TodoItems",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_Workspaces_WorkspaceId",
                table: "TodoItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TodoItems",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_WorkspaceId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "TodoItems");

            migrationBuilder.RenameTable(
                name: "TodoItems",
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
    }
}
