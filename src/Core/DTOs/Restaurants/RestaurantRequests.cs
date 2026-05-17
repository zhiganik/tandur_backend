namespace Core.DTOs.Restaurants;

public class CreateRestaurantRequest
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string TimeZone { get; init; } = string.Empty;
    public TimeSpan OpenTime { get; init; }
    public TimeSpan CloseTime { get; init; }
}

public class UpdateRestaurantRequest
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public TimeSpan OpenTime { get; init; }
    public TimeSpan CloseTime { get; init; }
}

public class PatchRestaurantRequest
{
    public bool? IsActive { get; init; }
}
