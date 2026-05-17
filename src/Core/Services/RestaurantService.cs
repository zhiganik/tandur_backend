using Core.Domain.Entities;
using Core.DTOs.Restaurants;
using Core.Interfaces;
using Core.Interfaces.Repositories;

namespace Core.Services;

public class RestaurantService(IRestaurantRepository repository) : IRestaurantService
{
    public async Task<IReadOnlyList<RestaurantDto>> GetAllAsync(double? lat, double? lng)
    {
        var restaurants = await repository.GetActiveAsync();

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
            Name = request.Name,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TimeZone = request.TimeZone,
            OpenTime = request.OpenTime,
            CloseTime = request.CloseTime
        };

        await repository.AddAsync(restaurant);
        return ToDto(restaurant, null, null);
    }

    public async Task<RestaurantDto?> UpdateAsync(Guid id, UpdateRestaurantRequest request)
    {
        var restaurant = await repository.GetByIdAsync(id);
        if (restaurant is null) return null;

        restaurant.Name = request.Name;
        restaurant.Address = request.Address;
        restaurant.Latitude = request.Latitude;
        restaurant.Longitude = request.Longitude;
        restaurant.OpenTime = request.OpenTime;
        restaurant.CloseTime = request.CloseTime;

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

    private static RestaurantDto ToDto(Restaurant r, double? lat, double? lng) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Address = r.Address,
        Latitude = r.Latitude,
        Longitude = r.Longitude,
        TimeZone = r.TimeZone,
        OpenTime = r.OpenTime,
        CloseTime = r.CloseTime,
        IsActive = r.IsActive,
        IsOpenNow = r.IsOpenNow(),
        DistanceKm = lat.HasValue && lng.HasValue ? r.DistanceTo(lat.Value, lng.Value) : null
    };
}
