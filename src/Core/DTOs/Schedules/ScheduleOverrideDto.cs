namespace Core.DTOs.Schedules;

public class ScheduleOverrideDto
{
    public Guid                   Id        { get; init; }
    public DateOnly               Date      { get; init; }
    public string                 Reason    { get; init; } = string.Empty;
    public bool                   IsInstant { get; init; }
    public IReadOnlyList<TimeSlotDto> TimeSlots { get; init; } = [];
}
