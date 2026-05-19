using Core.DTOs.Restaurants;

namespace Core.DTOs.Users;

public class UpdateProfileRequest
{
    public string FirstName   { get; init; } = string.Empty;
    public string LastName    { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}

public class UserDto
{
    public string                              Id                   { get; init; } = string.Empty;
    public string                              FirstName            { get; init; } = string.Empty;
    public string                              LastName             { get; init; } = string.Empty;
    public string?                             Email                { get; init; }
    public string?                             Phone                { get; init; }
    public bool                                EmailConfirmed       { get; init; }
    public bool                                PhoneNumberConfirmed { get; init; }
    public IReadOnlyList<string>               Roles                { get; init; } = [];
    public DateTime                            CreatedAt            { get; init; }
    public IReadOnlyList<RestaurantSummaryDto> Restaurants          { get; init; } = [];
}
