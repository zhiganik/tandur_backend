namespace Core.Domain.Entities;

public class RestaurantScheduleOverride
{
    public Guid          Id                { get; set; } = Guid.NewGuid();
    public Guid          RestaurantId      { get; set; }
    public DateOnly      Date              { get; set; }
    public string        Reason            { get; set; } = string.Empty;
    public List<TimeSlot> TimeSlots        { get; set; } = [];
    public DateTime      CreatedAt         { get; set; } = DateTime.UtcNow;
    public string        CreatedByAdminId  { get; set; } = string.Empty;

    public Restaurant Restaurant         { get; set; } = null!;
    public AppUser    CreatedByAdmin     { get; set; } = null!;

    // true when the override was created on the same day it applies (emergency closure)
    public bool IsInstant => DateOnly.FromDateTime(CreatedAt) == Date;

    public bool IsOpenAt(TimeSpan time)
    {
        if (TimeSlots.Count == 0) return false;
        return TimeSlots.Any(s => s.Contains(time));
    }
}
