using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow_API.Migrations
{
    /// <inheritdoc />
    public partial class AddStateArchivadoAndSpanishCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Administrador");

            migrationBuilder.UpdateData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Usuario Común");

            migrationBuilder.UpdateData(
                table: "ProjectRoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Creador");

            migrationBuilder.UpdateData(
                table: "ProjectRoles",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Gestor de Proyecto");

            migrationBuilder.UpdateData(
                table: "ProjectRoles",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Desarrollador");

            migrationBuilder.UpdateData(
                table: "ProjectStatuses",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Activo");

            migrationBuilder.UpdateData(
                table: "ProjectStatuses",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Completado");

            migrationBuilder.UpdateData(
                table: "ProjectStatuses",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "En Pausa");

            migrationBuilder.UpdateData(
                table: "ProjectStatuses",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Cancelado");

            migrationBuilder.InsertData(
                table: "ProjectStatuses",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { 5, null, "Archivado" });

            migrationBuilder.UpdateData(
                table: "TaskPriorities",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Baja");

            migrationBuilder.UpdateData(
                table: "TaskPriorities",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Media");

            migrationBuilder.UpdateData(
                table: "TaskPriorities",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Alta");

            migrationBuilder.UpdateData(
                table: "TaskPriorities",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Crítica");

            migrationBuilder.UpdateData(
                table: "TaskStatuses",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Por Hacer");

            migrationBuilder.UpdateData(
                table: "TaskStatuses",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "En Progreso");

            migrationBuilder.UpdateData(
                table: "TaskStatuses",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Hecho");

            migrationBuilder.UpdateData(
                table: "TaskStatuses",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Bloqueado");

            migrationBuilder.UpdateData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Funcionalidad");

            migrationBuilder.UpdateData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Error");

            migrationBuilder.UpdateData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Mejora");

            migrationBuilder.UpdateData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Investigación");

            migrationBuilder.UpdateData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "Tarea");

            migrationBuilder.UpdateData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "Subtarea");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProjectStatuses",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.UpdateData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Admin");

            migrationBuilder.UpdateData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "CommonUser");

            migrationBuilder.UpdateData(
                table: "ProjectRoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Creator");

            migrationBuilder.UpdateData(
                table: "ProjectRoles",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Project Manager");

            migrationBuilder.UpdateData(
                table: "ProjectRoles",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Developer");

            migrationBuilder.UpdateData(
                table: "ProjectStatuses",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Active");

            migrationBuilder.UpdateData(
                table: "ProjectStatuses",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Completed");

            migrationBuilder.UpdateData(
                table: "ProjectStatuses",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "On Hold");

            migrationBuilder.UpdateData(
                table: "ProjectStatuses",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Cancelled");

            migrationBuilder.UpdateData(
                table: "TaskPriorities",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "LOW");

            migrationBuilder.UpdateData(
                table: "TaskPriorities",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "MEDIUM");

            migrationBuilder.UpdateData(
                table: "TaskPriorities",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "HIGH");

            migrationBuilder.UpdateData(
                table: "TaskPriorities",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "CRITICAL");

            migrationBuilder.UpdateData(
                table: "TaskStatuses",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "To Do");

            migrationBuilder.UpdateData(
                table: "TaskStatuses",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "In Progress");

            migrationBuilder.UpdateData(
                table: "TaskStatuses",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Done");

            migrationBuilder.UpdateData(
                table: "TaskStatuses",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Blocked");

            migrationBuilder.UpdateData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Feature");

            migrationBuilder.UpdateData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Bug");

            migrationBuilder.UpdateData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Improvement");

            migrationBuilder.UpdateData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Research");

            migrationBuilder.UpdateData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "Task");

            migrationBuilder.UpdateData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "SubTask");
        }
    }
}
