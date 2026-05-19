using Core.Domain.Entities;
using Core.DTOs.Schedules;
using Core.Interfaces;
using Core.Interfaces.Repositories;

namespace Core.Services;

public class ScheduleService(IScheduleRepository scheduleRepo, IRestaurantRepository restaurantRepo) : IScheduleService
{
    public async Task<IReadOnlyList<ScheduleDayDto>> GetScheduleAsync(Guid restaurantId)
    {
        var schedule = await scheduleRepo.GetFullScheduleAsync(restaurantId);
        return schedule.Select(ToScheduleDayDto).ToList();
    }

    public async Task<IReadOnlyList<ScheduleDayDto>> UpdateFullScheduleAsync(Guid restaurantId, UpdateFullScheduleRequest request)
    {
        var rows = request.Days.Select(d => new RestaurantSchedule
        {
            Id           = Guid.NewGuid(),
            RestaurantId = restaurantId,
            DayOfWeek    = d.DayOfWeek,
            IsDayOff     = d.IsDayOff,
            TimeSlots    = d.TimeSlots.Select(ToTimeSlot).ToList(),
        }).ToList();

        await scheduleRepo.ReplaceFullScheduleAsync(restaurantId, rows);
        return rows.Select(ToScheduleDayDto).ToList();
    }

    public async Task<ScheduleDayDto?> UpdateScheduleDayAsync(Guid restaurantId, DayOfWeek day, UpdateScheduleDayRequest request)
    {
        var row = await scheduleRepo.GetScheduleDayAsync(restaurantId, day);
        if (row is null) return null;

        row.IsDayOff  = request.IsDayOff;
        row.TimeSlots = request.TimeSlots.Select(ToTimeSlot).ToList();

        await scheduleRepo.UpdateScheduleDayAsync(row);
        return ToScheduleDayDto(row);
    }

    public async Task<IReadOnlyList<ScheduleOverrideDto>> GetOverridesAsync(Guid restaurantId)
    {
        var overrides = await scheduleRepo.GetOverridesAsync(restaurantId);
        return overrides.Select(ToOverrideDto).ToList();
    }

    public async Task<ScheduleOverrideDto> CreateOverrideAsync(Guid restaurantId, CreateOverrideRequest request, string adminId)
    {
        var o = new RestaurantScheduleOverride
        {
            RestaurantId     = restaurantId,
            Date             = request.Date,
            Reason           = request.Reason,
            TimeSlots        = request.TimeSlots.Select(ToTimeSlot).ToList(),
            CreatedAt        = DateTime.UtcNow,
            CreatedByAdminId = adminId,
        };
        await scheduleRepo.AddOverrideAsync(o);
        return ToOverrideDto(o);
    }

    public async Task<ScheduleOverrideDto?> UpdateOverrideAsync(Guid overrideId, UpdateOverrideRequest request)
    {
        var o = await scheduleRepo.GetOverrideByIdAsync(overrideId);
        if (o is null) return null;

        o.Reason    = request.Reason;
        o.TimeSlots = request.TimeSlots.Select(ToTimeSlot).ToList();

        await scheduleRepo.UpdateOverrideAsync(o);
        return ToOverrideDto(o);
    }

    public Task<bool> DeleteOverrideAsync(Guid overrideId) =>
        scheduleRepo.DeleteOverrideAsync(overrideId);

    public async Task<ScheduleOverrideDto> CloseNowAsync(Guid restaurantId, InstantCloseRequest request, string adminId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = await scheduleRepo.GetTodayInstantOverrideAsync(restaurantId, today);

        if (existing is not null)
        {
            existing.Reason    = request.Reason;
            existing.TimeSlots = request.TimeSlots.Select(ToTimeSlot).ToList();
            await scheduleRepo.UpdateOverrideAsync(existing);
            return ToOverrideDto(existing);
        }

        var o = new RestaurantScheduleOverride
        {
            RestaurantId     = restaurantId,
            Date             = today,
            Reason           = request.Reason,
            TimeSlots        = request.TimeSlots.Select(ToTimeSlot).ToList(),
            CreatedAt        = DateTime.UtcNow,
            CreatedByAdminId = adminId,
        };
        await scheduleRepo.AddOverrideAsync(o);
        return ToOverrideDto(o);
    }

    public async Task<bool> ReopenNowAsync(Guid restaurantId)
    {
        var today    = DateOnly.FromDateTime(DateTime.UtcNow);
        var override_ = await scheduleRepo.GetTodayInstantOverrideAsync(restaurantId, today);
        if (override_ is null) return false;
        return await scheduleRepo.DeleteOverrideAsync(override_.Id);
    }

    public async Task<RestaurantSchedulePublicDto> GetPublicScheduleAsync(Guid restaurantId)
    {
        var restaurant = await restaurantRepo.GetByIdAsync(restaurantId);
        if (restaurant is null)
            return new RestaurantSchedulePublicDto();

        var schedule  = await scheduleRepo.GetFullScheduleAsync(restaurantId);
        var overrides = await scheduleRepo.GetOverridesAsync(restaurantId);

        restaurant.Schedules = (ICollection<RestaurantSchedule>)schedule;
        restaurant.Overrides = overrides
            .Where(o => o.Date == DateOnly.FromDateTime(DateTime.UtcNow))
            .Cast<RestaurantScheduleOverride>()
            .ToList();

        var isOpenNow = restaurant.IsOpenNow();

        var tz        = TryGetTimeZone(restaurant.TimeZone);
        var localNow  = tz is not null ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz) : DateTime.UtcNow;
        var today     = DateOnly.FromDateTime(localNow);
        var timeOfDay = localNow.TimeOfDay;

        var todaySlots = GetTodaySlots(today, schedule, overrides);

        var nextSlot = todaySlots
            .Where(s => s.To > timeOfDay)
            .OrderBy(s => s.From)
            .Select(s => new TimeSlotDto { From = s.From, To = s.To })
            .FirstOrDefault();

        var upcoming = overrides
            .Where(o => o.Date > today && o.TimeSlots.Count == 0)
            .OrderBy(o => o.Date)
            .Take(5)
            .Select(o => new UpcomingClosureDto { Date = o.Date, Reason = o.Reason })
            .ToList();

        return new RestaurantSchedulePublicDto
        {
            IsOpenNow        = isOpenNow,
            TodaySlots       = todaySlots.Select(s => new TimeSlotDto { From = s.From, To = s.To }).ToList(),
            NextSlot         = nextSlot,
            UpcomingClosures = upcoming,
        };
    }

    private static IReadOnlyList<TimeSlot> GetTodaySlots(
        DateOnly today,
        IEnumerable<RestaurantSchedule> schedule,
        IEnumerable<RestaurantScheduleOverride> overrides)
    {
        var todayOverride = overrides.FirstOrDefault(o => o.Date == today);
        if (todayOverride != null) return todayOverride.TimeSlots;

        var todaySchedule = schedule.FirstOrDefault(s => s.DayOfWeek == today.DayOfWeek);
        return todaySchedule?.TimeSlots ?? [];
    }

    private static TimeZoneInfo? TryGetTimeZone(string tz)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(tz); } catch { return null; }
    }

    private static ScheduleDayDto ToScheduleDayDto(RestaurantSchedule s) => new()
    {
        DayOfWeek = s.DayOfWeek,
        IsDayOff  = s.IsDayOff,
        TimeSlots = s.TimeSlots.Select(t => new TimeSlotDto { From = t.From, To = t.To }).ToList(),
    };

    private static ScheduleOverrideDto ToOverrideDto(RestaurantScheduleOverride o) => new()
    {
        Id        = o.Id,
        Date      = o.Date,
        Reason    = o.Reason,
        IsInstant = o.IsInstant,
        TimeSlots = o.TimeSlots.Select(t => new TimeSlotDto { From = t.From, To = t.To }).ToList(),
    };

    private static TimeSlot ToTimeSlot(TimeSlotRequest r) => new() { From = r.From, To = r.To };
}
