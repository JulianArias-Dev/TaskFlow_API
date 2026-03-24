using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow_API.Migrations
{
    /// <inheritdoc />
    public partial class Design2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_AssignedToUserId",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "AssignedToUserId",
                table: "Tasks",
                newName: "AssignedToId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_AssignedToUserId",
                table: "Tasks",
                newName: "IX_Tasks_AssignedToId");

            migrationBuilder.CreateTable(
                name: "TaskAssignment",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignment", x => new { x.TaskId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TaskAssignment_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskAssignment_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignment_UserId",
                table: "TaskAssignment",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_AssignedToId",
                table: "Tasks",
                column: "AssignedToId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_AssignedToId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "TaskAssignment");

            migrationBuilder.RenameColumn(
                name: "AssignedToId",
                table: "Tasks",
                newName: "AssignedToUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_AssignedToId",
                table: "Tasks",
                newName: "IX_Tasks_AssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_AssignedToUserId",
                table: "Tasks",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
