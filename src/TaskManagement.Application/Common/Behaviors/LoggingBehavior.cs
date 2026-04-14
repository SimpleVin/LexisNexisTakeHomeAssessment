using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TaskManagement.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        logger.LogDebug("Handling {MediatrRequest}", name);
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            logger.LogInformation(
                "Handled {MediatrRequest} in {ElapsedMs}ms",
                name,
                sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogDebug(
                ex,
                "Failed {MediatrRequest} after {ElapsedMs}ms",
                name,
                sw.ElapsedMilliseconds);
            throw;
        }
    }
}
