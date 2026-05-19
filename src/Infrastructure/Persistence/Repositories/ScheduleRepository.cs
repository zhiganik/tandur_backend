using Core.Domain.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ScheduleRepository(AppDbContext db) : IScheduleRepository
{
    public Task<IReadOnlyList<RestaurantSchedule>> GetFullScheduleAsync(Guid restaurantId) =>
        db.RestaurantSchedules
            .Where(s => s.RestaurantId == restaurantId)
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<RestaurantSchedule>)t.Result);

    public Task<RestaurantSchedule?> GetScheduleDayAsync(Guid restaurantId, DayOfWeek day) =>
        db.RestaurantSchedules
            .FirstOrDefaultAsync(s => s.RestaurantId == restaurantId && s.DayOfWeek == day)!;

    public async Task ReplaceFullScheduleAsync(Guid restaurantId, IReadOnlyList<RestaurantSchedule> schedule)
    {
        var existing = await db.RestaurantSchedules
            .Where(s => s.RestaurantId == restaurantId).ToListAsync();
        db.RestaurantSchedules.RemoveRange(existing);
        db.RestaurantSchedules.AddRange(schedule);
        await db.SaveChangesAsync();
    }

    public Task UpdateScheduleDayAsync(RestaurantSchedule schedule) => db.SaveChangesAsync();

    public async Task SeedDefaultScheduleAsync(Guid restaurantId)
    {
        var defaultSlot = new List<TimeSlot>
        {
            new() { From = TimeSpan.FromHours(9), To = TimeSpan.FromHours(22) },
        };

        var rows = Enum.GetValues<DayOfWeek>().Select(day => new RestaurantSchedule
        {
            Id           = Guid.NewGuid(),
            RestaurantId = restaurantId,
            DayOfWeek    = day,
            IsDayOff     = day == DayOfWeek.Sunday,
            TimeSlots    = day == DayOfWeek.Sunday ? [] : [.. defaultSlot],
        });

        db.RestaurantSchedules.AddRange(rows);
        await db.SaveChangesAsync();
    }

    public Task<IReadOnlyList<RestaurantScheduleOverride>> GetOverridesAsync(Guid restaurantId) =>
        db.RestaurantScheduleOverrides
            .Where(o => o.RestaurantId == restaurantId)
            .OrderBy(o => o.Date)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<RestaurantScheduleOverride>)t.Result);

    public Task<RestaurantScheduleOverride?> GetOverrideByIdAsync(Guid id) =>
        db.RestaurantScheduleOverrides.FindAsync(id).AsTask()!;

    public async Task<RestaurantScheduleOverride?> GetTodayInstantOverrideAsync(Guid restaurantId, DateOnly date)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd   = dayStart.AddDays(1);
        return await db.RestaurantScheduleOverrides
            .FirstOrDefaultAsync(o =>
                o.RestaurantId == restaurantId &&
                o.Date         == date         &&
                o.CreatedAt    >= dayStart      &&
                o.CreatedAt    <  dayEnd);
    }

    public async Task<RestaurantScheduleOverride> AddOverrideAsync(RestaurantScheduleOverride o)
    {
        db.RestaurantScheduleOverrides.Add(o);
        await db.SaveChangesAsync();
        return o;
    }

    public Task UpdateOverrideAsync(RestaurantScheduleOverride o) => db.SaveChangesAsync();

    public async Task<bool> DeleteOverrideAsync(Guid id)
    {
        var o = await db.RestaurantScheduleOverrides.FindAsync(id);
        if (o is null) return false;
        db.RestaurantScheduleOverrides.Remove(o);
        await db.SaveChangesAsync();
        return true;
    }
}
