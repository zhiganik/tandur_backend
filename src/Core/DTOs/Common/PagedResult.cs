namespace Core.DTOs.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Data      { get; init; } = [];
    public int              Total     { get; init; }
    public int              Page      { get; init; }
    public int              Limit     { get; init; }
    public int              TotalPages => Limit == 0 ? 0 : (int)Math.Ceiling((double)Total / Limit);
}
