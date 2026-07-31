namespace Durably.Queries;
/// <summary>A page of query results with total count for UI paging.</summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int TotalCount { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; }
}
