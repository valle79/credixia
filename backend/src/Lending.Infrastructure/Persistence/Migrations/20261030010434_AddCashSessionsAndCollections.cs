using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lending.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashSessionsAndCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cash_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    opening_balance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    opening_balance_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    current_balance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    current_balance_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    closing_balance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    closing_balance_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    physical_count_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    physical_count_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    difference_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    difference_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollectionActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PromiseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    District = table.Column<string>(type: "text", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: true),
                    Department = table.Column<string>(type: "text", nullable: true),
                    Occupation = table.Column<string>(type: "text", nullable: true),
                    Employer = table.Column<string>(type: "text", nullable: true),
                    MonthlyIncome = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "loan_products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    InterestType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    minimum_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    maximum_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumTermDays = table.Column<int>(type: "integer", nullable: false),
                    MaximumTermDays = table.Column<int>(type: "integer", nullable: false),
                    CommissionPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    RequiresGuarantor = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "loans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    InterestType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TermDays = table.Column<int>(type: "integer", nullable: false),
                    NumberOfPayments = table.Column<int>(type: "integer", nullable: false),
                    DisbursementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FirstPaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    commission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    commission_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    total_interest = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_interest_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    balance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApprovedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisbursedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DisbursedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cash_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: true),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CashSessionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cash_transactions_cash_sessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "cash_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Relationship = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_references", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_references_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_schedule_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentNumber = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    due_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    late_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    late_fee_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaidDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_schedule_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_schedule_items_loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsReversed = table.Column<bool>(type: "boolean", nullable: false),
                    ReversedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReversedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversalReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payments_loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cash_sessions_Status",
                table: "cash_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cash_sessions_TenantId",
                table: "cash_sessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_sessions_TenantId_UserId_SessionDate",
                table: "cash_sessions",
                columns: new[] { "TenantId", "UserId", "SessionDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cash_sessions_UserId",
                table: "cash_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_transactions_CashSessionId",
                table: "cash_transactions",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_transactions_LoanId",
                table: "cash_transactions",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_transactions_SessionId",
                table: "cash_transactions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_references_CustomerId",
                table: "customer_references",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customers_TenantId",
                table: "customers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_customers_TenantId_DocumentNumber",
                table: "customers",
                columns: new[] { "TenantId", "DocumentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loan_products_TenantId",
                table: "loan_products",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_loan_products_TenantId_Code",
                table: "loan_products",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loans_CustomerId",
                table: "loans",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_loans_Status",
                table: "loans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_loans_TenantId",
                table: "loans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_loans_TenantId_LoanNumber",
                table: "loans",
                columns: new[] { "TenantId", "LoanNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_schedule_items_LoanId",
                table: "payment_schedule_items",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_schedule_items_LoanId_PaymentNumber",
                table: "payment_schedule_items",
                columns: new[] { "LoanId", "PaymentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_LoanId",
                table: "payments",
                column: "LoanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cash_transactions");

            migrationBuilder.DropTable(
                name: "CollectionActions");

            migrationBuilder.DropTable(
                name: "customer_references");

            migrationBuilder.DropTable(
                name: "loan_products");

            migrationBuilder.DropTable(
                name: "payment_schedule_items");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "cash_sessions");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "loans");
        }
    }
}
