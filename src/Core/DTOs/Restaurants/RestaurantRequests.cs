namespace Core.DTOs.Restaurants;

public class CreateRestaurantRequest
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string TimeZone { get; init; } = string.Empty;
}

public class UpdateRestaurantRequest
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string Currency { get; init; } = string.Empty;
}

public class PatchRestaurantRequest
{
    public bool? IsActive { get; init; }
}
