using Core.Domain.Enums;

namespace Core.Interfaces;

public interface IRefreshTokenService
{
    Task<string>                             CreateAsync(string userId, ClientType clientType);
    Task<(string? UserId, ClientType? Type)> GetAsync(string token);
    Task                                     RevokeAsync(string token);
    Task                                     RevokeAllForUserAsync(string userId);
}
