namespace Core.DTOs.Common;

public class PaginationQuery
{
    public int Page  { get; init; } = 1;
    public int Limit { get; init; } = 20;
}
