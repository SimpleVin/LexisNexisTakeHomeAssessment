using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskManagement.Api.Swagger;

internal sealed class AnonymousSwaggerRoutesDocumentFilter : IDocumentFilter
{
    private static readonly string[] AnonymousPathPrefixes = ["/api/dev", "/health"];

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var path in swaggerDoc.Paths)
        {
            if (!AnonymousPathPrefixes.Any(p => path.Key.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            foreach (var operation in path.Value.Operations.Values)
            {
                operation.Security.Clear();
            }
        }
    }
}
