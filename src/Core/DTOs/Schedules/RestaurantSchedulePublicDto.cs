namespace Core.DTOs.Schedules;

public class RestaurantSchedulePublicDto
{
    public bool                        IsOpenNow        { get; init; }
    public IReadOnlyList<TimeSlotDto>  TodaySlots       { get; init; } = [];
    public TimeSlotDto?                NextSlot         { get; init; }
    public IReadOnlyList<UpcomingClosureDto> UpcomingClosures { get; init; } = [];
}

public class UpcomingClosureDto
{
    public DateOnly Date   { get; init; }
    public string   Reason { get; init; } = string.Empty;
}
