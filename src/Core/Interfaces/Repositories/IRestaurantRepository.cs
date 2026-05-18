using Core.Domain.Entities;

namespace Core.Interfaces.Repositories;

public interface IRestaurantRepository
{
    Task<IReadOnlyList<Restaurant>> GetPagedActiveAsync(int page, int limit);
    Task<int>                       CountActiveAsync();
    Task<IReadOnlyList<Restaurant>> GetPagedAllAsync(int page, int limit);
    Task<int>                       CountAllAsync();
    Task<Restaurant?>               GetByIdAsync(Guid id);
    Task<Restaurant>                AddAsync(Restaurant restaurant);
    Task                            UpdateAsync(Restaurant restaurant);
    Task<bool>                      SoftDeleteAsync(Guid id);
}
