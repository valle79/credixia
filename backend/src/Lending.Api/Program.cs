using System.Text;
using System.Threading.RateLimiting;
using Lending.Api.Authentication;
using Lending.Api.Configuration;
using Lending.Api.Endpoints;
using Lending.Api.Infrastructure;
using Lending.Api.Middleware;
using Lending.Api.Observability;
using Lending.Api.Tenancy;
using Lending.Application.Abstractions;
using Lending.Infrastructure;
using Lending.Infrastructure.Health;
using Lending.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

    builder.WebHost.ConfigureKestrel(options =>
        options.Limits.MaxRequestBodySize = 10 * 1024 * 1024);

    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddScoped<RequestTenantContext>();
    builder.Services.AddScoped<ITenantContext>(provider =>
        provider.GetRequiredService<RequestTenantContext>());

    var jwtOptions = builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>() ?? new JwtOptions();
    jwtOptions.Validate();

    var authOptions = builder.Configuration
        .GetSection(AuthOptions.SectionName)
        .Get<AuthOptions>() ?? new AuthOptions();
    authOptions.Validate();

    builder.Services.Configure<JwtOptions>(
        builder.Configuration.GetSection(JwtOptions.SectionName));
    builder.Services.Configure<AuthOptions>(
        builder.Configuration.GetSection(AuthOptions.SectionName));
    builder.Services.Configure<BootstrapOptions>(
        builder.Configuration.GetSection(BootstrapOptions.SectionName));

    builder.Services.AddScoped<CurrentUser>();
    builder.Services.AddScoped<ICurrentUser>(provider => provider.GetRequiredService<CurrentUser>());
    builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
    builder.Services.AddSingleton(new ServerRuntimeInfo(DateTimeOffset.UtcNow));
    builder.Services.AddScoped<SessionTokenService>();
    builder.Services.AddScoped<TenantProvisioner>();
    builder.Services.AddScoped<PlatformAdminSeeder>();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddOpenApi();

    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }

        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders(CorrelationIdMiddleware.HeaderName);
    }));

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        
        // Only apply rate limiting in non-development environments
        if (!builder.Environment.IsDevelopment())
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    clientKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    });
            });
        }

        options.AddPolicy("auth", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = builder.Environment.IsDevelopment() ? 100 : 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
    });

    builder.Services
        .AddHealthChecks()
        .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

    builder.Services.AddLendingObservability(builder.Configuration);

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("Referer", httpContext.Request.Headers.Referer.ToString());
            diagnosticContext.Set("SecFetchMode", httpContext.Request.Headers["Sec-Fetch-Mode"].ToString());
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        };
    });

    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    app.UseCors("frontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseMiddleware<SessionContextMiddleware>();
    app.UseAuthorization();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
    }).AllowAnonymous();

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
    }).AllowAnonymous();

    var exposeOpenApi = app.Environment.IsDevelopment()
        || app.Configuration.GetValue<bool>("Api:ExposeOpenApi");

    if (exposeOpenApi)
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    await using (var scope = app.Services.CreateAsyncScope())
    {
        if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            await scope.ServiceProvider
                .GetRequiredService<LendingDbContext>()
                .Database
                .MigrateAsync();
        }

        await scope.ServiceProvider
            .GetRequiredService<PlatformAdminSeeder>()
            .SeedAsync();
    }

    app.MapSystemEndpoints();
    app.MapAuthEndpoints();
    app.MapMembershipEndpoints();
    app.MapTenantAdministrationEndpoints();
    app.MapPlatformEndpoints();
    app.MapPlatformSettingsEndpoints();
    app.MapDashboardEndpoints();
    app.MapCustomersEndpoints();
    app.MapLoanProductsEndpoints();
    app.MapLoansEndpoints();
    app.MapCashEndpoints();
    app.MapCollectionsEndpoints();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
