using Lending.Application.Abstractions;

namespace Lending.Api.Authorization;

public sealed class RequirePermissionFilter : IEndpointFilter
{
    private readonly string _permissionCode;

    public RequirePermissionFilter(string permissionCode)
    {
        _permissionCode = permissionCode;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var currentUser = context.HttpContext.RequestServices
            .GetRequiredService<ICurrentUser>();

        if (!currentUser.IsAuthenticated)
        {
            return TypedResults.Unauthorized();
        }

        if (!currentUser.HasPermission(_permissionCode))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Permiso insuficiente",
                detail: $"La operación requiere el permiso '{_permissionCode}'.");
        }

        return await next(context);
    }
}

public static class PermissionEndpointExtensions
{
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder,
        string permissionCode)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEndpointFilter(new RequirePermissionFilter(permissionCode));
    }
}
