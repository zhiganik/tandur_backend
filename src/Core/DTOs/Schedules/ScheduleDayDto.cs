namespace Core.DTOs.Schedules;

public class ScheduleDayDto
{
    public DayOfWeek              DayOfWeek { get; init; }
    public bool                   IsDayOff  { get; init; }
    public IReadOnlyList<TimeSlotDto> TimeSlots { get; init; } = [];
}
