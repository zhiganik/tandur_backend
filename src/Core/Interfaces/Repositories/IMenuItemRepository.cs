using Core.Domain.Entities;

namespace Core.Interfaces.Repositories;

public interface IMenuItemRepository
{
    Task<IReadOnlyList<MenuItem>> GetAvailableByRestaurantAsync(Guid restaurantId);
    Task<IReadOnlyList<MenuItem>> GetAllByRestaurantAsync(Guid restaurantId);
    Task<MenuItem?> GetByIdAsync(Guid id);
    Task<MenuItem> AddAsync(MenuItem item);
    Task UpdateAsync(MenuItem item);
    Task<bool> SoftDeleteAsync(Guid id);
}
