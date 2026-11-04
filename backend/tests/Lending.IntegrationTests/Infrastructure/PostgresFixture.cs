using Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Lending.IntegrationTests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("lending_tests")
        .WithUsername("lending")
        .WithPassword("lending_test_only")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        await context.Database.ExecuteSqlRawAsync(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'lending_rls') THEN
                    CREATE ROLE lending_rls NOLOGIN;
                END IF;
            END
            $$;
            GRANT USAGE ON SCHEMA public TO lending_rls;
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO lending_rls;
            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO lending_rls;
            """);
    }

    public async Task DisposeAsync() => await Container.DisposeAsync();

    public LendingDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<LendingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);
}

[CollectionDefinition(Name)]
public sealed class PostgresFixtureDefinition : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
