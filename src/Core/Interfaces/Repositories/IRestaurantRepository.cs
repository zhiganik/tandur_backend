using Core.Domain.Entities;

namespace Core.Interfaces.Repositories;

public interface IRestaurantRepository
{
    Task<IReadOnlyList<Restaurant>> GetActiveAsync();
    Task<IReadOnlyList<Restaurant>> GetAllAsync();
    Task<Restaurant?> GetByIdAsync(Guid id);
    Task<Restaurant> AddAsync(Restaurant restaurant);
    Task UpdateAsync(Restaurant restaurant);
    Task<bool> SoftDeleteAsync(Guid id);
}
