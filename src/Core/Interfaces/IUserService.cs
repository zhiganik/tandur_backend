using Core.DTOs.Common;
using Core.DTOs.Users;

namespace Core.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserDto>>  GetPagedAsync(PaginationQuery query);
    Task<UserUpdateResult>      UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<bool>                  DeleteAsync(string userId);
}
