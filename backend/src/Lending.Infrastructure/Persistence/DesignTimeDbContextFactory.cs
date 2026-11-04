using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lending.Infrastructure.Persistence;

/// <summary>
/// Used by `dotnet ef` only. Runtime configuration is provided by the API host.
/// The fallback is a local development database, never a production credential.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LendingDbContext>
{
    private const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=55432;Database=lending;Username=lending;Password=lending_dev_only";

    public LendingDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? LocalDevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<LendingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new LendingDbContext(options);
    }
}
