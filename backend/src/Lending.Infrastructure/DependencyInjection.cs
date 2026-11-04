using Lending.Application.Abstractions;
using Lending.Infrastructure.Audit;
using Lending.Infrastructure.Documents;
using Lending.Infrastructure.Email;
using Lending.Infrastructure.Persistence;
using Lending.Infrastructure.Platform;
using Lending.Infrastructure.Security;
using Lending.Infrastructure.Storage;
using Lending.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Lending.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Obtener la cadena de conexión PostgreSQL.
        var connectionString = configuration.GetConnectionString("Postgres");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Postgres is required. Configure it through environment variables or a secret store.");
        }

        // Configurar el origen de datos PostgreSQL.
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

        dataSourceBuilder.ConnectionStringBuilder.ApplicationName ??= "lending-api";

        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);

        // Registrar Entity Framework Core con PostgreSQL.
        // Configurar SplitQuery como comportamiento predeterminado.
        services.AddDbContext<LendingDbContext>((serviceProvider, options) =>
        {
            var postgresDataSource =
                serviceProvider.GetRequiredService<NpgsqlDataSource>();

            options.UseNpgsql(
                postgresDataSource,
                npgsqlOptions =>
                {
                    npgsqlOptions.UseQuerySplittingBehavior(
                        QuerySplittingBehavior.SplitQuery
                    );
                }
            );
        });

        // Servicios de infraestructura.
        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();

        services.AddSingleton<ISecretProtector, AesGcmSecretProtector>();
        services.AddScoped<PlatformSettingsStore>();
        services.AddScoped<PlatformNotificationService>();
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<IEmailSender>(provider =>
            provider.GetRequiredService<SmtpEmailSender>());

        services.AddScoped<IAuditWriter, AuditWriter>();

        // Cloudinary
        var cloudinarySection = configuration.GetSection("Cloudinary");
        services.Configure<CloudinaryOptions>(cloudinarySection);
        services.AddSingleton<IFileUploadService, CloudinaryFileUploadService>();

        // PDF Generation
        services.AddScoped<IPaymentSchedulePdfGenerator, PaymentSchedulePdfGenerator>();

        return services;
    }
}
