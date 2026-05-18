using Core.Domain.Entities;

namespace Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<IReadOnlyList<AppUser>>                    GetPagedAsync(int page, int limit);
    Task<int>                                       CountAsync();
    Task<AppUser?>                                  GetByIdAsync(string id);
    Task<Dictionary<string, IReadOnlyList<string>>> GetRolesMapAsync(IEnumerable<string> userIds);
    Task<IReadOnlyList<string>>                     GetRolesAsync(string userId);
    Task<(bool Success, string[] Errors)>           UpdateAsync(AppUser user);
    Task<bool>                                      DeleteAsync(string userId);
}
