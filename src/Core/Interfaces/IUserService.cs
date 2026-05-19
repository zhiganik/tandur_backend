using Core.DTOs.Common;
using Core.DTOs.Me;
using Core.DTOs.Users;

namespace Core.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserDto>>  GetPagedAsync(PaginationQuery query, bool maskPii);
    Task<UserDto?>              GetByIdAsync(string userId);
    Task<UserUpdateResult>      UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<bool>                  DeleteAsync(string userId);
    Task<MeDto?>                GetMeAsync(string userId);
    Task<UserUpdateResult>      UpdateNameAsync(string userId, string firstName, string lastName);
    Task<UserUpdateResult>      SetVerifiedPhoneAsync(string userId, string newPhone);
    Task<UserUpdateResult>      SetVerifiedEmailAsync(string userId, string newEmail);
    Task<UserUpdateResult>      SetBirthdayAsync(string userId, DateTime? birthday);
}
