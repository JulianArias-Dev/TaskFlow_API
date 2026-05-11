using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow_API.Migrations
{
    /// <inheritdoc />
    public partial class SeedSuperAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AppRoleId", "AvatarUrl", "CreatedAt", "Email", "IsActive", "LastLoginAt", "Name", "NotificationPreferences", "NotifyByEmail", "PasswordHash", "UpdatedAt" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), 1, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "superadmin@taskflow.com", true, null, "SuperAdmin", null, true, "$2a$11$QiuTL7x1k/8hGeh4GXfhZOT2WD3lkz7S2EYkUS6rMJsAa71X1883m", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));
        }
    }
}
