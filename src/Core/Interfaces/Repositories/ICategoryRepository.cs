using Core.Domain.Entities;

namespace Core.Interfaces.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetVisibleByRestaurantAsync(Guid restaurantId);
    Task<IReadOnlyList<Category>> GetAllByRestaurantAsync(Guid restaurantId);
    Task<Category?> GetByIdAsync(Guid id);
    Task<Category> AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> HasActiveItemsAsync(Guid categoryId);
}
