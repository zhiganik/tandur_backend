namespace Core.Domain.Entities;

public class Restaurant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<MenuItem> MenuItems { get; set; } = [];
    public ICollection<AppUser> AssignedAdmins { get; set; } = [];

    public bool IsOpenNow()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).TimeOfDay;
            return localNow >= OpenTime && localNow < CloseTime;
        }
        catch
        {
            return false;
        }
    }

    public double DistanceTo(double lat, double lng)
    {
        var dLat = ToRad(lat - Latitude);
        var dLon = ToRad(lng - Longitude);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(Latitude)) * Math.Cos(ToRad(lat))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 6371 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
