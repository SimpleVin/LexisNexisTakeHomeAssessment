namespace TaskManagement.Application.Common.Contracts.Pagination;

public static class Pagination
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
    {
        var p = page is null or < 1 ? 1 : page.Value;
        var s = pageSize is null or < 1 ? DefaultPageSize : Math.Min(pageSize.Value, MaxPageSize);
        return (p, s);
    }
}
