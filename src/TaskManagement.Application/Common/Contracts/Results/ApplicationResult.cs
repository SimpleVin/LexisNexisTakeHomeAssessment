namespace TaskManagement.Application.Common.Contracts.Results;

public sealed record ApplicationResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<ApplicationError> Errors { get; init; } = [];

    public static ApplicationResult<T> Ok(T data) =>
        new() { Success = true, Data = data, Errors = [] };

    public static ApplicationResult<T> Fail(IReadOnlyList<ApplicationError> errors) =>
        new()
        {
            Success = false,
            Data = default,
            Errors = errors.Count > 0
                ? errors
                : [new ApplicationError(ApplicationErrorCodes.Error, "The operation failed.")],
        };

    public static ApplicationResult<T> Fail(string code, string message) =>
        Fail([new ApplicationError(code, message)]);

    public static ApplicationResult<T> Fail(params ApplicationError[] errors) =>
        Fail((IReadOnlyList<ApplicationError>)errors);
}
