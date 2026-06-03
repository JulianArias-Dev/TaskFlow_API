using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow_API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSubTareaType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reasignamos cualquier tarea existente con TypeId=6 ("Subtarea")
            // al tipo neutro "Tarea" (Id=5) ANTES de borrar la fila del
            // catálogo. Sin esto, el DELETE viola la FK Tasks.TypeId.
            migrationBuilder.Sql("UPDATE Tasks SET TypeId = 5 WHERE TypeId = 6");

            migrationBuilder.DeleteData(
                table: "TaskTypes",
                keyColumn: "Id",
                keyValue: 6);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "TaskTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { 6, null, "Subtarea" });
        }
    }
}
