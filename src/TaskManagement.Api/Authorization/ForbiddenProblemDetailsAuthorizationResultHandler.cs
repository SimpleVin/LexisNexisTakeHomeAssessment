using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace TaskManagement.Api.Authorization;

public sealed class ForbiddenProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        if (!(context.User.Identity?.IsAuthenticated ?? false))
        {
            await context.ChallengeAsync();
            return;
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        const string detail = "You do not have permission for this request.";
        var problem = new
        {
            title = "Forbidden",
            status = 403,
            detail,
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
