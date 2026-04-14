using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common.Contracts.Results;

namespace TaskManagement.Api.Extensions;

public static class ApplicationResultExtensions
{
    public static ActionResult<ApplicationResult<T>> ToActionResult<T>(this ApplicationResult<T> result)
    {
        if (result.Success)
        {
            return new OkObjectResult(result);
        }

        return new ObjectResult(result) { StatusCode = ResolveStatusCode(result.Errors) };
    }
  public static ActionResult<ApplicationResult<T>> ToCreatedAtActionResult<T>(
        this ApplicationResult<T> result,
        string actionName,
        Func<T, object> buildRouteValues)
    {
        if (!result.Success)
        {
            return result.ToActionResult();
        }

        return new CreatedAtActionResult(actionName, null, buildRouteValues(result.Data!), result);
    }

    public static IActionResult ToActionResult(this ApplicationUnitResult result)
    {
        if (result.Success)
        {
            return new OkObjectResult(result);
        }

        return new ObjectResult(result) { StatusCode = ResolveStatusCode(result.Errors) };
    }

    private static int ResolveStatusCode(IReadOnlyList<ApplicationError> errors)
    {
        if (errors.Any(e => string.Equals(e.Code, ApplicationErrorCodes.NotFound, StringComparison.OrdinalIgnoreCase)))
        {
            return StatusCodes.Status404NotFound;
        }

        if (errors.Any(e => string.Equals(e.Code, ApplicationErrorCodes.Conflict, StringComparison.OrdinalIgnoreCase)))
        {
            return StatusCodes.Status409Conflict;
        }

        if (errors.Any(e => string.Equals(e.Code, ApplicationErrorCodes.Forbidden, StringComparison.OrdinalIgnoreCase)))
        {
            return StatusCodes.Status403Forbidden;
        }

        return StatusCodes.Status400BadRequest;
    }
}
