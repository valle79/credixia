using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lending.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefinancingAndSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RefinancedFromLoanId",
                table: "loans",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "early_settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    outstanding_principal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    outstanding_principal_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    accrued_interest = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    accrued_interest_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    settlement_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    settlement_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_paid_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SettlementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_early_settlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "loan_refinancings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalLoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewLoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    outstanding_principal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    outstanding_principal_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    accrued_interest = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    accrued_interest_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_debt = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_debt_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    new_principal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    new_principal_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    NewInterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    RefinancingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_refinancings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_early_settlements_LoanId",
                table: "early_settlements",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_early_settlements_TenantId",
                table: "early_settlements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_refinancings_NewLoanId",
                table: "loan_refinancings",
                column: "NewLoanId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_refinancings_OriginalLoanId",
                table: "loan_refinancings",
                column: "OriginalLoanId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_refinancings_TenantId",
                table: "loan_refinancings",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "early_settlements");

            migrationBuilder.DropTable(
                name: "loan_refinancings");

            migrationBuilder.DropColumn(
                name: "RefinancedFromLoanId",
                table: "loans");
        }
    }
}
