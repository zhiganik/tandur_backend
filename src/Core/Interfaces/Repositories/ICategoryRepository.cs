using Core.Domain.Entities;

namespace Core.Interfaces.Repositories;

public interface ICategoryRepository
{
    // Non-paged — used internally by MenuItemService to build composite MenuDto
    Task<IReadOnlyList<Category>> GetVisibleByRestaurantAsync(Guid restaurantId);
    Task<IReadOnlyList<Category>> GetAllByRestaurantAsync(Guid restaurantId);

    // Paged — used by list endpoints
    Task<IReadOnlyList<Category>> GetPagedVisibleAsync(Guid restaurantId, int page, int limit);
    Task<int>                     CountVisibleAsync(Guid restaurantId);
    Task<IReadOnlyList<Category>> GetPagedAllAsync(Guid restaurantId, int page, int limit);
    Task<int>                     CountAllAsync(Guid restaurantId);

    Task<Category?>  GetByIdAsync(Guid id);
    Task<Category>   AddAsync(Category category);
    Task             UpdateAsync(Category category);
    Task<bool>       DeleteAsync(Guid id);
    Task<bool>       HasActiveItemsAsync(Guid categoryId);
}
