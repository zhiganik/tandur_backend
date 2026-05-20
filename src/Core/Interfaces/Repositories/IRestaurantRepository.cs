using Core.Domain.Entities;

namespace Core.Interfaces.Repositories;

public interface IRestaurantRepository
{
    Task<IReadOnlyList<Restaurant>> GetAllActiveAsync();
    Task<IReadOnlyList<Restaurant>> GetAllAsync();
    Task<Restaurant?>               GetByIdAsync(Guid id);
    Task<Restaurant>                AddAsync(Restaurant restaurant);
    Task                            UpdateAsync(Restaurant restaurant);
    Task<bool>                      SoftDeleteAsync(Guid id);
    Task<IReadOnlyList<Restaurant>> GetByAdminAsync(string adminUserId);
    Task<bool>                                           AssignToAdminAsync(Guid restaurantId, string adminUserId);
    Task<bool>                                           UnassignFromAdminAsync(Guid restaurantId, string adminUserId);
}
