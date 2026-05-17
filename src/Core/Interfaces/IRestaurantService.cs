using Core.DTOs.Restaurants;

namespace Core.Interfaces;

public interface IRestaurantService
{
    Task<IReadOnlyList<RestaurantDto>> GetAllAsync(double? lat, double? lng);
    Task<IReadOnlyList<RestaurantDto>> GetAdminListAsync();
    Task<RestaurantDto?> GetByIdAsync(Guid id);
    Task<RestaurantDto> CreateAsync(CreateRestaurantRequest request);
    Task<RestaurantDto?> UpdateAsync(Guid id, UpdateRestaurantRequest request);
    Task<RestaurantDto?> PatchAsync(Guid id, PatchRestaurantRequest request);
    Task<bool> DeleteAsync(Guid id);
}
