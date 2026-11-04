using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lending.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(LendingDbContext))]
    [Migration("20261031120000_PerformanceIndexesAndFinancialRls")]
    public partial class PerformanceIndexesAndFinancialRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'payment_schedule_items'
                          AND column_name = 'LoanId')
                    AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'payment_schedule_items'
                          AND column_name = 'loan_id') THEN
                        ALTER TABLE payment_schedule_items RENAME COLUMN "LoanId" TO loan_id;
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'payments'
                          AND column_name = 'LoanId')
                    AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'payments'
                          AND column_name = 'loan_id') THEN
                        ALTER TABLE payments RENAME COLUMN "LoanId" TO loan_id;
                    END IF;
                END $$;

                CREATE EXTENSION IF NOT EXISTS pg_trgm;

                CREATE INDEX IF NOT EXISTS ix_loans_tenant_status_created
                    ON loans ("TenantId", "Status", "CreatedAt");
                CREATE INDEX IF NOT EXISTS ix_loans_tenant_created
                    ON loans ("TenantId", "CreatedAt");
                CREATE INDEX IF NOT EXISTS ix_loans_tenant_customer_created
                    ON loans ("TenantId", "CustomerId", "CreatedAt");
                CREATE INDEX IF NOT EXISTS ix_loans_tenant_disbursed
                    ON loans ("TenantId", "DisbursedDate");
                CREATE INDEX IF NOT EXISTS ix_schedule_loan_due
                    ON payment_schedule_items (loan_id, "DueDate");
                CREATE INDEX IF NOT EXISTS ix_schedule_due_unpaid
                    ON payment_schedule_items ("DueDate")
                    WHERE paid_amount < due_amount;
                CREATE INDEX IF NOT EXISTS ix_payments_loan_payment_date
                    ON payments (loan_id, "PaymentDate");
                CREATE INDEX IF NOT EXISTS ix_collection_actions_loan_registered
                    ON "CollectionActions" ("LoanId", "RegisteredAt");
                CREATE INDEX IF NOT EXISTS ix_cash_sessions_tenant_status_opened
                    ON cash_sessions ("TenantId", "Status", "OpenedAt");
                CREATE INDEX IF NOT EXISTS ix_cash_transactions_session_registered
                    ON cash_transactions ("CashSessionId", "RegisteredAt");
                CREATE INDEX IF NOT EXISTS ix_customers_tenant_fullname
                    ON customers ("TenantId", "FullName");

                CREATE INDEX IF NOT EXISTS ix_customers_fullname_trgm
                    ON customers USING gin ("FullName" gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS ix_customers_document_trgm
                    ON customers USING gin ("DocumentNumber" gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS ix_loans_loan_number_trgm
                    ON loans USING gin ("LoanNumber" gin_trgm_ops);

                ALTER TABLE loans ENABLE ROW LEVEL SECURITY;
                ALTER TABLE loans FORCE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS loans_tenant ON loans;
                CREATE POLICY loans_tenant ON loans FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    );

                ALTER TABLE customers ENABLE ROW LEVEL SECURITY;
                ALTER TABLE customers FORCE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS customers_tenant ON customers;
                CREATE POLICY customers_tenant ON customers FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    );

                ALTER TABLE loan_products ENABLE ROW LEVEL SECURITY;
                ALTER TABLE loan_products FORCE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS loan_products_tenant ON loan_products;
                CREATE POLICY loan_products_tenant ON loan_products FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    );

                ALTER TABLE cash_sessions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE cash_sessions FORCE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS cash_sessions_tenant ON cash_sessions;
                CREATE POLICY cash_sessions_tenant ON cash_sessions FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    );

                ALTER TABLE payment_schedule_items ENABLE ROW LEVEL SECURITY;
                ALTER TABLE payment_schedule_items FORCE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS payment_schedule_items_tenant ON payment_schedule_items;
                CREATE POLICY payment_schedule_items_tenant ON payment_schedule_items FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM loans
                            WHERE loans."Id" = payment_schedule_items.loan_id
                              AND loans."TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM loans
                            WHERE loans."Id" = payment_schedule_items.loan_id
                              AND loans."TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    );

                ALTER TABLE payments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE payments FORCE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS payments_tenant ON payments;
                CREATE POLICY payments_tenant ON payments FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM loans
                            WHERE loans."Id" = payments.loan_id
                              AND loans."TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM loans
                            WHERE loans."Id" = payments.loan_id
                              AND loans."TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    );

                ALTER TABLE "CollectionActions" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "CollectionActions" FORCE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS collection_actions_tenant ON "CollectionActions";
                CREATE POLICY collection_actions_tenant ON "CollectionActions" FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM loans
                            WHERE loans."Id" = "CollectionActions"."LoanId"
                              AND loans."TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM loans
                            WHERE loans."Id" = "CollectionActions"."LoanId"
                              AND loans."TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    );

                ALTER TABLE customer_references ENABLE ROW LEVEL SECURITY;
                ALTER TABLE customer_references FORCE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS customer_references_tenant ON customer_references;
                CREATE POLICY customer_references_tenant ON customer_references FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM customers
                            WHERE customers."Id" = customer_references."CustomerId"
                              AND customers."TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM customers
                            WHERE customers."Id" = customer_references."CustomerId"
                              AND customers."TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    );

                ALTER TABLE cash_transactions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE cash_transactions FORCE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS cash_transactions_tenant ON cash_transactions;
                CREATE POLICY cash_transactions_tenant ON cash_transactions FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM cash_sessions
                            WHERE cash_sessions."Id" = cash_transactions."CashSessionId"
                              AND cash_sessions."TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM cash_sessions
                            WHERE cash_sessions."Id" = cash_transactions."CashSessionId"
                              AND cash_sessions."TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS cash_transactions_tenant ON cash_transactions;
                ALTER TABLE cash_transactions NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE cash_transactions DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS customer_references_tenant ON customer_references;
                ALTER TABLE customer_references NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE customer_references DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS collection_actions_tenant ON "CollectionActions";
                ALTER TABLE "CollectionActions" NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE "CollectionActions" DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS payments_tenant ON payments;
                ALTER TABLE payments NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE payments DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS payment_schedule_items_tenant ON payment_schedule_items;
                ALTER TABLE payment_schedule_items NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE payment_schedule_items DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS cash_sessions_tenant ON cash_sessions;
                ALTER TABLE cash_sessions NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE cash_sessions DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS loan_products_tenant ON loan_products;
                ALTER TABLE loan_products NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE loan_products DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS customers_tenant ON customers;
                ALTER TABLE customers NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE customers DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS loans_tenant ON loans;
                ALTER TABLE loans NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE loans DISABLE ROW LEVEL SECURITY;

                DROP INDEX IF EXISTS ix_loans_loan_number_trgm;
                DROP INDEX IF EXISTS ix_customers_document_trgm;
                DROP INDEX IF EXISTS ix_customers_fullname_trgm;
                DROP INDEX IF EXISTS ix_customers_tenant_fullname;
                DROP INDEX IF EXISTS ix_cash_transactions_session_registered;
                DROP INDEX IF EXISTS ix_cash_sessions_tenant_status_opened;
                DROP INDEX IF EXISTS ix_collection_actions_loan_registered;
                DROP INDEX IF EXISTS ix_payments_loan_payment_date;
                DROP INDEX IF EXISTS ix_schedule_due_unpaid;
                DROP INDEX IF EXISTS ix_schedule_loan_due;
                DROP INDEX IF EXISTS ix_loans_tenant_disbursed;
                DROP INDEX IF EXISTS ix_loans_tenant_customer_created;
                DROP INDEX IF EXISTS ix_loans_tenant_created;
                DROP INDEX IF EXISTS ix_loans_tenant_status_created;
                """);
        }
    }
}
