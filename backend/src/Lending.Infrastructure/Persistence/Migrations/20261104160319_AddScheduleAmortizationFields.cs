using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lending.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleAmortizationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "interest_amount",
                table: "payment_schedule_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "interest_currency",
                table: "payment_schedule_items",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "principal_amount",
                table: "payment_schedule_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "principal_currency",
                table: "payment_schedule_items",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "remaining_balance",
                table: "payment_schedule_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "remaining_balance_currency",
                table: "payment_schedule_items",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "interest_amount",
                table: "payment_schedule_items");

            migrationBuilder.DropColumn(
                name: "interest_currency",
                table: "payment_schedule_items");

            migrationBuilder.DropColumn(
                name: "principal_amount",
                table: "payment_schedule_items");

            migrationBuilder.DropColumn(
                name: "principal_currency",
                table: "payment_schedule_items");

            migrationBuilder.DropColumn(
                name: "remaining_balance",
                table: "payment_schedule_items");

            migrationBuilder.DropColumn(
                name: "remaining_balance_currency",
                table: "payment_schedule_items");
        }
    }
}
