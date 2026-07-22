namespace Predictathon.Application.Models;

/// <summary>
/// A server-paged slice of a larger result set, together with enough information for the caller
/// to render pagination controls.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
