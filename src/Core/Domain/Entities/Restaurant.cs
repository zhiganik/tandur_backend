namespace Core.Domain.Entities;

public class Restaurant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category>                   Categories { get; set; } = [];
    public ICollection<MenuItem>                   MenuItems  { get; set; } = [];
    public ICollection<AppUser>                    AssignedAdmins { get; set; } = [];
    public ICollection<RestaurantSchedule>         Schedules  { get; set; } = [];
    public ICollection<RestaurantScheduleOverride> Overrides  { get; set; } = [];

    public bool IsOpenNow(DateTime? now = null)
    {
        try
        {
            var tz        = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(now ?? DateTime.UtcNow, tz);
            var today     = DateOnly.FromDateTime(localTime);
            var timeOfDay = localTime.TimeOfDay;

            var todayOverride = Overrides.FirstOrDefault(o => o.Date == today);
            if (todayOverride != null)
                return todayOverride.IsOpenAt(timeOfDay);

            var todaySchedule = Schedules.FirstOrDefault(s => s.DayOfWeek == localTime.DayOfWeek);
            return todaySchedule?.IsOpenAt(timeOfDay) ?? false;
        }
        catch { return false; }
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
