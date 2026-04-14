using System.Net;
using System.Text.Json;
using FluentValidation;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning("Not found: {Message}", ex.Message);
            await WriteHttpProblemDetailsJson(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (BadRequestException ex)
        {
            logger.LogWarning("Bad request: {Message}", ex.Message);
            await WriteHttpProblemDetailsJson(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation failed with {ErrorCount} error(s)", ex.Errors.Count());
            await WriteHttpValidationProblemDetailsJson(context, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteHttpProblemDetailsJson(context, HttpStatusCode.InternalServerError, "Something went wrong.");
        }
    }

    private static Task WriteHttpProblemDetailsJson(HttpContext context, HttpStatusCode status, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;
        var problem = new
        {
            title = status.ToString(),
            status = (int)status,
            detail,
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }

    private static Task WriteHttpValidationProblemDetailsJson(HttpContext context, ValidationException ex)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        var problem = new
        {
            title = "Validation failed.",
            status = 400,
            errors,
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
