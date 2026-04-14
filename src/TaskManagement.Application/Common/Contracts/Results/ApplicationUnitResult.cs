namespace TaskManagement.Application.Common.Contracts.Results;

public sealed record ApplicationUnitResult
{
    public bool Success { get; init; }
    
    public string? Message { get; init; }
    public IReadOnlyList<ApplicationError> Errors { get; init; } = [];

    public static ApplicationUnitResult Ok(string? message = null) =>
        new() { Success = true, Message = message, Errors = [] };

    public static ApplicationUnitResult Fail(IReadOnlyList<ApplicationError> errors) =>
        new()
        {
            Success = false,
            Errors = errors.Count > 0
                ? errors
                : [new ApplicationError(ApplicationErrorCodes.Error, "The operation failed.")],
        };

    public static ApplicationUnitResult Fail(string code, string message) =>
        Fail([new ApplicationError(code, message)]);

    public static ApplicationUnitResult Fail(params ApplicationError[] errors) =>
        Fail((IReadOnlyList<ApplicationError>)errors);
}
