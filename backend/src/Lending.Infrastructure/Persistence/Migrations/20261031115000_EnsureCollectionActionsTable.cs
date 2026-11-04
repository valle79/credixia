using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lending.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(LendingDbContext))]
    [Migration("20261031115000_EnsureCollectionActionsTable")]
    public partial class EnsureCollectionActionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "CollectionActions" (
                    "Id" uuid NOT NULL,
                    "LoanId" uuid NOT NULL,
                    "CollectorUserId" uuid NOT NULL,
                    "Type" integer NOT NULL,
                    "Notes" text NULL,
                    "PromiseDate" date NULL,
                    "RegisteredAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_CollectionActions" PRIMARY KEY ("Id")
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
