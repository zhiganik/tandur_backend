namespace Core.DTOs.Users;

public class UserQuery
{
    public int          Page         { get; init; } = 1;
    public int          Limit        { get; init; } = 20;
    public string?      Search       { get; init; }
    public List<string> Roles        { get; init; } = [];
    public Guid?        RestaurantId { get; init; }
    public string       Sort         { get; init; } = "desc";
}
