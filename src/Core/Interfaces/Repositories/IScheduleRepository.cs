using Core.Domain.Entities;

namespace Core.Interfaces.Repositories;

public interface IScheduleRepository
{
    Task<IReadOnlyList<RestaurantSchedule>>         GetFullScheduleAsync(Guid restaurantId);
    Task<RestaurantSchedule?>                       GetScheduleDayAsync(Guid restaurantId, DayOfWeek day);
    Task                                            ReplaceFullScheduleAsync(Guid restaurantId, IReadOnlyList<RestaurantSchedule> schedule);
    Task                                            UpdateScheduleDayAsync(RestaurantSchedule schedule);
    Task                                            SeedDefaultScheduleAsync(Guid restaurantId);

    Task<IReadOnlyList<RestaurantScheduleOverride>> GetOverridesAsync(Guid restaurantId);
    Task<RestaurantScheduleOverride?>               GetOverrideByIdAsync(Guid id);
    Task<RestaurantScheduleOverride?>               GetTodayInstantOverrideAsync(Guid restaurantId, DateOnly date);
    Task<RestaurantScheduleOverride>                AddOverrideAsync(RestaurantScheduleOverride o);
    Task                                            UpdateOverrideAsync(RestaurantScheduleOverride o);
    Task<bool>                                      DeleteOverrideAsync(Guid id);
}
