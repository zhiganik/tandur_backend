using Core.Domain.Entities;

namespace Core.Interfaces.Repositories;

public interface IMenuItemRepository
{
    Task<IReadOnlyList<MenuItem>> GetAllAvailableAsync(Guid restaurantId);
    Task<IReadOnlyList<MenuItem>> GetAllAsync(Guid restaurantId);
    Task<IReadOnlyList<MenuItem>> GetByIdsAsync(IReadOnlyList<Guid> ids);
    Task<int>                     GetMaxSortOrderAsync(Guid restaurantId);
    Task<MenuItem?>               GetByIdAsync(Guid id);
    Task<MenuItem>                AddAsync(MenuItem item);
    Task                          UpdateAsync(MenuItem item);
    Task<bool>                    SoftDeleteAsync(Guid id);
}
