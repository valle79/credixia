using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lending.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlatformSettingsAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    link_url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    min_password_length = table.Column<int>(type: "integer", nullable: false),
                    require_letter_and_digit = table.Column<bool>(type: "boolean", nullable: false),
                    max_failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    lockout_minutes = table.Column<int>(type: "integer", nullable: false),
                    refresh_token_days = table.Column<int>(type: "integer", nullable: false),
                    email_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    email_host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email_port = table.Column<int>(type: "integer", nullable: false),
                    email_use_ssl = table.Column<bool>(type: "boolean", nullable: false),
                    email_username = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    email_password_ciphertext = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    email_from_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    email_from_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    email_last_test_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    email_last_test_succeeded = table.Column<bool>(type: "boolean", nullable: true),
                    email_last_test_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notify_tenant_created = table.Column<bool>(type: "boolean", nullable: false),
                    notify_tenant_suspended = table.Column<bool>(type: "boolean", nullable: false),
                    notify_tenant_reactivated = table.Column<bool>(type: "boolean", nullable: false),
                    notify_suspicious_access = table.Column<bool>(type: "boolean", nullable: false),
                    notify_email_failures = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_platform_notifications_created_at",
                table: "platform_notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_platform_notifications_is_read",
                table: "platform_notifications",
                column: "is_read");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_notifications");

            migrationBuilder.DropTable(
                name: "platform_settings");
        }
    }
}
