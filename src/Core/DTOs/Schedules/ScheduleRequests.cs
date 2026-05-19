namespace Core.DTOs.Schedules;

public class TimeSlotRequest
{
    public TimeSpan From { get; init; }
    public TimeSpan To   { get; init; }
}

public class UpdateScheduleDayRequest
{
    public bool                  IsDayOff  { get; init; }
    public List<TimeSlotRequest> TimeSlots { get; init; } = [];
}

public class UpdateFullScheduleDayRequest
{
    public DayOfWeek             DayOfWeek { get; init; }
    public bool                  IsDayOff  { get; init; }
    public List<TimeSlotRequest> TimeSlots { get; init; } = [];
}

public class UpdateFullScheduleRequest
{
    public List<UpdateFullScheduleDayRequest> Days { get; init; } = [];
}

public class CreateOverrideRequest
{
    public DateOnly              Date      { get; init; }
    public string                Reason    { get; init; } = string.Empty;
    public List<TimeSlotRequest> TimeSlots { get; init; } = [];
}

public class UpdateOverrideRequest
{
    public string                Reason    { get; init; } = string.Empty;
    public List<TimeSlotRequest> TimeSlots { get; init; } = [];
}

public class InstantCloseRequest
{
    public string                Reason    { get; init; } = string.Empty;
    public List<TimeSlotRequest> TimeSlots { get; init; } = [];
}
