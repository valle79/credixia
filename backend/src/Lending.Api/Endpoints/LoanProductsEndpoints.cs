using Lending.Api.Authentication;
using Lending.Api.Authorization;
using Lending.Application.Abstractions;
using Lending.Domain.Common;
using Lending.Domain.Identity;
using Lending.Domain.LoanProducts;
using Lending.Domain.ValueObjects;
using Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lending.Api.Endpoints;

public static class LoanProductsEndpoints
{
    public static IEndpointRouteBuilder MapLoanProductsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/loan-products")
            .WithTags("LoanProducts")
            .RequireAuthorization();

        group.MapGet("/", ListLoanProductsAsync)
            .WithName("ListLoanProducts")
            .RequirePermission(Permissions.LoansView);

        group.MapGet("/{id:guid}", GetLoanProductAsync)
            .WithName("GetLoanProduct")
            .RequirePermission(Permissions.LoansView);

        group.MapPost("/", CreateLoanProductAsync)
            .WithName("CreateLoanProduct")
            .RequirePermission(Permissions.SettingsManage);

        group.MapPut("/{id:guid}", UpdateLoanProductAsync)
            .WithName("UpdateLoanProduct")
            .RequirePermission(Permissions.SettingsManage);

        group.MapPatch("/{id:guid}/status", UpdateLoanProductStatusAsync)
            .WithName("UpdateLoanProductStatus")
            .RequirePermission(Permissions.SettingsManage);

        group.MapDelete("/{id:guid}", DeleteLoanProductAsync)
            .WithName("DeleteLoanProduct")
            .RequirePermission(Permissions.SettingsManage);

        return endpoints;
    }

    private static async Task<IResult> ListLoanProductsAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        bool? active,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var query = dbContext.LoanProducts
            .AsNoTracking()
            .Where(p => p.TenantId == currentUser.TenantId.Value);

        if (active.HasValue)
        {
            query = query.Where(p => p.IsActive == active.Value);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(products.Select(p => new LoanProductSummary(
            p.Id,
            p.Code,
            p.Name,
            p.Frequency.ToString(),
            p.InterestRate,
            p.InterestType.ToString(),
            p.MinimumAmount.Amount,
            p.MaximumAmount.Amount,
            p.MinimumTermDays,
            p.MaximumTermDays,
            p.MinimumAmount.Currency.Code,
            p.Description,
            p.IsActive)).ToArray());
    }

    private static async Task<IResult> GetLoanProductAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var product = await dbContext.LoanProducts
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id && p.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (product is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new LoanProductDetails(
            product.Id,
            product.Code,
            product.Name,
            product.Frequency.ToString(),
            product.InterestRate,
            product.InterestType.ToString(),
            product.MinimumAmount.Amount,
            product.MaximumAmount.Amount,
            product.MinimumTermDays,
            product.MaximumTermDays,
            product.MinimumAmount.Currency.Code,
            product.CommissionPercentage,
            product.FixedCommission?.Amount,
            product.RequiresGuarantor,
            product.Description,
            product.IsActive,
            product.CreatedAt));
    }

    private static async Task<IResult> CreateLoanProductAsync(
        CreateLoanProductRequest request,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        LoanProduct product;
        try
        {
            var currency = Currency.FromCode(request.Currency);
            product = LoanProduct.Create(
                currentUser.TenantId.Value,
                request.Code,
                request.Name,
                Enum.Parse<PaymentFrequency>(request.Frequency, true),
                request.InterestRate,
                Enum.Parse<InterestType>(request.InterestType, true),
                Money.Of(request.MinimumAmount, currency),
                Money.Of(request.MaximumAmount, currency),
                request.MinimumTermDays,
                request.MaximumTermDays,
                clock.UtcNow);

            Money? fixedCommission = request.FixedCommission.HasValue
                ? Money.Of(request.FixedCommission.Value, currency)
                : null;

            product.Update(
                request.Name,
                request.InterestRate,
                product.MinimumAmount,
                product.MaximumAmount,
                request.MinimumTermDays,
                request.MaximumTermDays,
                request.CommissionPercentage,
                fixedCommission,
                request.RequiresGuarantor ?? false,
                request.Description,
                clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Datos inválidos",
                detail: exception.Message);
        }

        dbContext.LoanProducts.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "loan_product.created",
                "LoanProduct",
                product.Id.ToString()),
            cancellationToken);

        return TypedResults.Created($"/api/v1/loan-products/{product.Id}", new { id = product.Id });
    }

    private static async Task<IResult> UpdateLoanProductAsync(
        Guid id,
        UpdateLoanProductRequest request,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var product = await dbContext.LoanProducts
            .SingleOrDefaultAsync(p => p.Id == id && p.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (product is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            var currency = Currency.FromCode(request.Currency);
            Money? fixedCommission = request.FixedCommission.HasValue
                ? Money.Of(request.FixedCommission.Value, currency)
                : null;

            product.Update(
                request.Name,
                request.InterestRate,
                Money.Of(request.MinimumAmount, currency),
                Money.Of(request.MaximumAmount, currency),
                request.MinimumTermDays,
                request.MaximumTermDays,
                request.CommissionPercentage,
                fixedCommission,
                request.RequiresGuarantor,
                request.Description,
                clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Datos inválidos",
                detail: exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "loan_product.updated",
                "LoanProduct",
                product.Id.ToString()),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> UpdateLoanProductStatusAsync(
        Guid id,
        UpdateLoanProductStatusRequest request,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var product = await dbContext.LoanProducts
            .SingleOrDefaultAsync(p => p.Id == id && p.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (product is null)
        {
            return TypedResults.NotFound();
        }

        if (request.Active)
        {
            product.Activate(clock.UtcNow);
        }
        else
        {
            product.Deactivate(clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                request.Active ? "loan_product.activated" : "loan_product.deactivated",
                "LoanProduct",
                product.Id.ToString()),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> DeleteLoanProductAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var tenantId = currentUser.TenantId.Value;
        var product = await dbContext.LoanProducts
            .SingleOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, cancellationToken);

        if (product is null)
        {
            return TypedResults.NotFound();
        }

        var hasLoans = await dbContext.Loans
            .AnyAsync(l => l.TenantId == tenantId && l.LoanProductId == id, cancellationToken);

        if (hasLoans)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Producto con préstamos",
                detail: "No se puede eliminar un producto que tiene préstamos asociados. Desactívalo en su lugar.");
        }

        dbContext.LoanProducts.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                tenantId,
                currentUser.UserId,
                "loan_product.deleted",
                "LoanProduct",
                id.ToString()),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static ProblemHttpResult TenantRequired() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Organización requerida",
            detail: "Selecciona una organización antes de continuar.");

    public sealed record LoanProductSummary(
        Guid Id,
        string Code,
        string Name,
        string Frequency,
        decimal InterestRate,
        string InterestType,
        decimal MinimumAmount,
        decimal MaximumAmount,
        int MinimumTermDays,
        int MaximumTermDays,
        string Currency,
        string? Description,
        bool IsActive);

    public sealed record LoanProductDetails(
        Guid Id,
        string Code,
        string Name,
        string Frequency,
        decimal InterestRate,
        string InterestType,
        decimal MinimumAmount,
        decimal MaximumAmount,
        int MinimumTermDays,
        int MaximumTermDays,
        string Currency,
        decimal? CommissionPercentage,
        decimal? FixedCommission,
        bool RequiresGuarantor,
        string? Description,
        bool IsActive,
        DateTimeOffset CreatedAt);

    public sealed record CreateLoanProductRequest(
        string Code,
        string Name,
        string Frequency,
        decimal InterestRate,
        string InterestType,
        decimal MinimumAmount,
        decimal MaximumAmount,
        int MinimumTermDays,
        int MaximumTermDays,
        string Currency,
        decimal? CommissionPercentage,
        decimal? FixedCommission,
        bool? RequiresGuarantor,
        string? Description);

    public sealed record UpdateLoanProductRequest(
        string Name,
        decimal InterestRate,
        decimal MinimumAmount,
        decimal MaximumAmount,
        int MinimumTermDays,
        int MaximumTermDays,
        string Currency,
        decimal? CommissionPercentage,
        decimal? FixedCommission,
        bool RequiresGuarantor,
        string? Description);

    public sealed record UpdateLoanProductStatusRequest(bool Active);
}
