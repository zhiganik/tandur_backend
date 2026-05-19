using Core.Domain.Entities;
using Core.DTOs.Users;

namespace Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<IReadOnlyList<AppUser>>                    GetPagedAsync(UserQuery query);
    Task<int>                                       CountAsync(UserQuery query);
    Task<AppUser?>                                  GetByIdAsync(string id);
    Task<Dictionary<string, IReadOnlyList<string>>> GetRolesMapAsync(IEnumerable<string> userIds);
    Task<IReadOnlyList<string>>                     GetRolesAsync(string userId);
    Task<(bool Success, string[] Errors)>           UpdateAsync(AppUser user);
    Task<bool>                                      DeleteAsync(string userId);
    Task<AppUser?>                                  GetByEmailAsync(string email);
    Task<AppUser?>                                  GetByPhoneAsync(string phone, string? excludeUserId = null);
    Task<(bool Success, string[] Errors)>           SetConfirmedPhoneAsync(string userId, string newPhone);
    Task<(bool Success, string[] Errors)>           SetConfirmedEmailAsync(string userId, string newEmail);
    Task<(bool Success, string[] Errors)>           SetBirthdayAsync(string userId, DateTime? birthday);
}
