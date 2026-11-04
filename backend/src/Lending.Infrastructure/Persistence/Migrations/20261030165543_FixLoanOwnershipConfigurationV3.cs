using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lending.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixLoanOwnershipConfigurationV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op migration: database schema already matches the model
            // The columns and constraints were already renamed in a previous migration
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op migration rollback
        }
    }
}
