using Core.Domain.Entities;
using Core.DTOs.Restaurants;
using Core.Interfaces;
using Core.Interfaces.Repositories;

namespace Core.Services;

public class RestaurantService(IRestaurantRepository repository, IScheduleRepository scheduleRepo) : IRestaurantService
{
    public async Task<IReadOnlyList<RestaurantDto>> GetAllAsync(double? lat, double? lng)
    {
        var restaurants = await repository.GetAllActiveAsync();

        return restaurants
            .Select(r => ToDto(r, lat, lng))
            .OrderBy(r => r.DistanceKm ?? double.MaxValue)
            .ToList();
    }

    public async Task<IReadOnlyList<RestaurantDto>> GetAdminListAsync()
    {
        var restaurants = await repository.GetAllAsync();
        return restaurants.Select(r => ToDto(r, null, null)).ToList();
    }

    public async Task<RestaurantDto?> GetByIdAsync(Guid id)
    {
        var r = await repository.GetByIdAsync(id);
        return r is null ? null : ToDto(r, null, null);
    }

    public async Task<RestaurantDto> CreateAsync(CreateRestaurantRequest request)
    {
        var restaurant = new Restaurant
        {
            Name      = request.Name,
            Address   = request.Address,
            Latitude  = request.Latitude,
            Longitude = request.Longitude,
            Currency  = request.Currency,
            TimeZone  = request.TimeZone,
        };

        await repository.AddAsync(restaurant);
        await scheduleRepo.SeedDefaultScheduleAsync(restaurant.Id);
        return ToDto(restaurant, null, null);
    }

    public async Task<RestaurantDto?> UpdateAsync(Guid id, UpdateRestaurantRequest request)
    {
        var restaurant = await repository.GetByIdAsync(id);
        if (restaurant is null) return null;

        restaurant.Name      = request.Name;
        restaurant.Address   = request.Address;
        restaurant.Latitude  = request.Latitude;
        restaurant.Longitude = request.Longitude;
        restaurant.Currency  = request.Currency;

        await repository.UpdateAsync(restaurant);
        return ToDto(restaurant, null, null);
    }

    public async Task<RestaurantDto?> PatchAsync(Guid id, PatchRestaurantRequest request)
    {
        var restaurant = await repository.GetByIdAsync(id);
        if (restaurant is null) return null;

        if (request.IsActive.HasValue)
            restaurant.IsActive = request.IsActive.Value;

        await repository.UpdateAsync(restaurant);
        return ToDto(restaurant, null, null);
    }

    public Task<bool> DeleteAsync(Guid id) => repository.SoftDeleteAsync(id);

    public async Task<IReadOnlyList<RestaurantSummaryDto>> GetAllSummariesAsync()
    {
        var restaurants = await repository.GetAllSummariesAsync();
        return restaurants.Select(r => new RestaurantSummaryDto(r.Id, r.Name)).ToList();
    }

    public async Task<IReadOnlyList<RestaurantSummaryDto>> GetSummariesForAdminAsync(string adminUserId)
    {
        var restaurants = await repository.GetByAdminAsync(adminUserId);
        return restaurants.Select(r => new RestaurantSummaryDto(r.Id, r.Name)).ToList();
    }

    public Task<bool> AssignToAdminAsync(Guid restaurantId, string adminUserId) =>
        repository.AssignToAdminAsync(restaurantId, adminUserId);

    public Task<bool> UnassignFromAdminAsync(Guid restaurantId, string adminUserId) =>
        repository.UnassignFromAdminAsync(restaurantId, adminUserId);

    private static RestaurantDto ToDto(Restaurant r, double? lat, double? lng) => new()
    {
        Id         = r.Id,
        Name       = r.Name,
        Address    = r.Address,
        Latitude   = r.Latitude,
        Longitude  = r.Longitude,
        Currency   = r.Currency,
        TimeZone   = r.TimeZone,
        IsActive   = r.IsActive,
        IsOpenNow  = r.IsOpenNow(),
        DistanceKm = lat.HasValue && lng.HasValue ? r.DistanceTo(lat.Value, lng.Value) : null,
    };
}
