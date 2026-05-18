using Core.Domain.Entities;

namespace Core.Interfaces.Repositories;

public interface IMenuItemRepository
{
    Task<IReadOnlyList<MenuItem>> GetPagedAvailableAsync(Guid restaurantId, int page, int limit);
    Task<int>                     CountAvailableAsync(Guid restaurantId);
    Task<IReadOnlyList<MenuItem>> GetPagedAllAsync(Guid restaurantId, int page, int limit);
    Task<int>                     CountAllAsync(Guid restaurantId);
    Task<MenuItem?>               GetByIdAsync(Guid id);
    Task<MenuItem>                AddAsync(MenuItem item);
    Task                          UpdateAsync(MenuItem item);
    Task<bool>                    SoftDeleteAsync(Guid id);
}
