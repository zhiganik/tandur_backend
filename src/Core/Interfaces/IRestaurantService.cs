using Core.DTOs.Common;
using Core.DTOs.Restaurants;

namespace Core.Interfaces;

public interface IRestaurantService
{
    Task<PagedResult<RestaurantDto>> GetAllAsync(double? lat, double? lng, PaginationQuery query);
    Task<PagedResult<RestaurantDto>> GetAdminListAsync(PaginationQuery query);
    Task<RestaurantDto?>             GetByIdAsync(Guid id);
    Task<RestaurantDto>              CreateAsync(CreateRestaurantRequest request);
    Task<RestaurantDto?>             UpdateAsync(Guid id, UpdateRestaurantRequest request);
    Task<RestaurantDto?>             PatchAsync(Guid id, PatchRestaurantRequest request);
    Task<bool>                       DeleteAsync(Guid id);
}
