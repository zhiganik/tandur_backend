namespace Core.Domain.Entities;

public class RestaurantSchedule
{
    public Guid        Id           { get; set; } = Guid.NewGuid();
    public Guid        RestaurantId { get; set; }
    public DayOfWeek   DayOfWeek    { get; set; }
    public bool        IsDayOff     { get; set; }
    public List<TimeSlot> TimeSlots { get; set; } = [];

    public Restaurant Restaurant { get; set; } = null!;

    public bool IsOpenAt(TimeSpan time)
    {
        if (IsDayOff || TimeSlots.Count == 0) return false;
        return TimeSlots.Any(s => s.Contains(time));
    }
}
