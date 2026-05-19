using Core.DTOs.Schedules;

namespace Core.Interfaces;

public interface IScheduleService
{
    Task<IReadOnlyList<ScheduleDayDto>>    GetScheduleAsync(Guid restaurantId);
    Task<IReadOnlyList<ScheduleDayDto>>    UpdateFullScheduleAsync(Guid restaurantId, UpdateFullScheduleRequest request);
    Task<ScheduleDayDto?>                  UpdateScheduleDayAsync(Guid restaurantId, DayOfWeek day, UpdateScheduleDayRequest request);

    Task<IReadOnlyList<ScheduleOverrideDto>> GetOverridesAsync(Guid restaurantId);
    Task<ScheduleOverrideDto>                CreateOverrideAsync(Guid restaurantId, CreateOverrideRequest request, string adminId);
    Task<ScheduleOverrideDto?>               UpdateOverrideAsync(Guid overrideId, UpdateOverrideRequest request);
    Task<bool>                               DeleteOverrideAsync(Guid overrideId);

    Task<ScheduleOverrideDto>  CloseNowAsync(Guid restaurantId, InstantCloseRequest request, string adminId);
    Task<bool>                 ReopenNowAsync(Guid restaurantId);

    Task<RestaurantSchedulePublicDto> GetPublicScheduleAsync(Guid restaurantId);
}
