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
    Task<IReadOnlyList<Restaurant>>                      GetAllSummariesAsync();
    Task<IReadOnlyList<Restaurant>>                      GetByAdminAsync(string adminUserId);
    Task<Dictionary<string, IReadOnlyList<Restaurant>>> GetByAdminsAsync(IEnumerable<string> adminUserIds);
    Task<bool>                                           AssignToAdminAsync(Guid restaurantId, string adminUserId);
    Task<bool>                                           UnassignFromAdminAsync(Guid restaurantId, string adminUserId);
}
