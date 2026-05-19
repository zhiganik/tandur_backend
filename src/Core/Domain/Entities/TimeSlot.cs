namespace Core.Domain.Entities;

public class TimeSlot
{
    public TimeSpan From { get; set; }
    public TimeSpan To   { get; set; }

    public bool Contains(TimeSpan time) => time >= From && time < To;
}
