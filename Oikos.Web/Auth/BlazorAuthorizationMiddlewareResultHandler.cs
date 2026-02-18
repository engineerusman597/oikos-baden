using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Oikos.Web.Auth;


// https://github.com/dotnet/aspnetcore/issues/52063
// AuthorizeRouteView does not work
public class BlazorAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        // Check if it's a custom authorization policy
        if (policy.Requirements.OfType<ApiAuthorizeRequirement>().Any() &&
            !authorizeResult.Succeeded)
        {
            // Return 401
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        return next(context);
    }
}
