using System.Text.Json;

namespace TaskManagement.Api.Authentication;

internal static class UnauthorizedProblemDetailsWriter
{
    public static Task WriteAsync(HttpResponse response, CancellationToken cancellationToken = default)
    {
        response.StatusCode = StatusCodes.Status401Unauthorized;
        response.ContentType = "application/problem+json";
        var problem = new
        {
            title = "Unauthorized",
            status = 401,
            detail = "Authentication failed.",
        };
        return response.WriteAsync(JsonSerializer.Serialize(problem), cancellationToken);
    }
}
