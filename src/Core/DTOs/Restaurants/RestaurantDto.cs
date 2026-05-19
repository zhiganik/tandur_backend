namespace Core.DTOs.Restaurants;

public class RestaurantDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string TimeZone { get; init; } = string.Empty;
    public TimeSpan OpenTime { get; init; }
    public TimeSpan CloseTime { get; init; }
    public bool IsActive { get; init; }
    public bool IsOpenNow { get; init; }
    public double? DistanceKm { get; init; }
}