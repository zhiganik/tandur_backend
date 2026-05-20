using Core.Domain.Entities;
using Core.DTOs.Common;
using Core.DTOs.Me;
using Core.DTOs.Restaurants;
using Core.DTOs.Users;
using Core.Interfaces;
using Core.Interfaces.Repositories;

namespace Core.Services;

public class UserService(
    IUserRepository repository,
    IRefreshTokenService refreshTokenService) : IUserService
{
    public async Task<PagedResult<UserDto>> GetPagedAsync(UserQuery query, bool maskPii)
    {
        var total    = await repository.CountAsync(query);
        var users    = await repository.GetPagedAsync(query);
        var rolesMap = await repository.GetRolesMapAsync(users.Select(u => u.Id));

        return new PagedResult<UserDto>
        {
            Data  = users.Select(u => ToDto(u, rolesMap.GetValueOrDefault(u.Id, []), maskPii)).ToList(),
            Total = total,
            Page  = query.Page,
            Limit = query.Limit,
        };
    }

    public async Task<UserDto?> GetByIdAsync(string userId)
    {
        var user = await repository.GetByIdAsync(userId);
        if (user is null) return null;
        var roles = await repository.GetRolesAsync(userId);
        return ToDto(user, roles, maskPii: false);
    }

    public async Task<bool> DeleteAsync(string userId)
    {
        await refreshTokenService.RevokeAllForUserAsync(userId);
        return await repository.DeleteAsync(userId);
    }

    public async Task<MeDto?> GetMeAsync(string userId)
    {
        var user = await repository.GetByIdAsync(userId);
        if (user is null) return null;
        var roles = await repository.GetRolesAsync(userId);
        return ToMeDto(user, roles);
    }

    public async Task<UserUpdateResult> UpdateNameAsync(string userId, string firstName, string lastName)
    {
        var user = await repository.GetByIdAsync(userId);
        if (user is null) return new UserUpdateResult.NotFound();
        user.FirstName = firstName;
        user.LastName  = lastName;
        var (success, errors) = await repository.UpdateAsync(user);
        return success ? new UserUpdateResult.Success() : new UserUpdateResult.Failed(errors);
    }

    public async Task<UserUpdateResult> SetVerifiedPhoneAsync(string userId, string newPhone)
    {
        var (success, errors) = await repository.SetConfirmedPhoneAsync(userId, newPhone);
        if (!success && errors.Contains("User not found.")) return new UserUpdateResult.NotFound();
        return success ? new UserUpdateResult.Success() : new UserUpdateResult.Failed(errors);
    }

    public async Task<UserUpdateResult> SetVerifiedEmailAsync(string userId, string newEmail)
    {
        var (success, errors) = await repository.SetConfirmedEmailAsync(userId, newEmail);
        if (!success && errors.Contains("User not found.")) return new UserUpdateResult.NotFound();
        return success ? new UserUpdateResult.Success() : new UserUpdateResult.Failed(errors);
    }

    public async Task<UserUpdateResult> SetBirthdayAsync(string userId, DateTime? birthday)
    {
        var (success, errors) = await repository.SetBirthdayAsync(userId, birthday);
        if (!success && errors.Contains("User not found.")) return new UserUpdateResult.NotFound();
        return success ? new UserUpdateResult.Success() : new UserUpdateResult.Failed(errors);
    }

    private static UserDto ToDto(AppUser user, IReadOnlyList<string> roles, bool maskPii) => new()
    {
        Id                   = user.Id,
        FirstName            = user.FirstName,
        LastName             = user.LastName,
        Email                = maskPii ? MaskEmail(user.Email) : user.Email,
        Phone                = maskPii ? MaskPhone(user.PhoneNumber) : user.PhoneNumber,
        EmailConfirmed       = user.EmailConfirmed,
        PhoneNumberConfirmed = user.PhoneNumberConfirmed,
        Roles                = roles,
        CreatedAt            = user.CreatedAt,
        Restaurants          = user.AssignedRestaurants.Select(ToRestaurantDto).ToList(),
    };

    private static MeDto ToMeDto(AppUser user, IReadOnlyList<string> roles) => new(
        Id:                   user.Id,
        FirstName:            user.FirstName,
        LastName:             user.LastName,
        Email:                user.Email,
        EmailConfirmed:       user.EmailConfirmed,
        Phone:                user.PhoneNumber,
        PhoneNumberConfirmed: user.PhoneNumberConfirmed,
        DateOfBirth:          user.DateOfBirth,
        Roles:                roles,
        CreatedAt:            user.CreatedAt,
        Restaurants:          user.AssignedRestaurants.Select(ToRestaurantDto).ToList()
    );

    private static RestaurantDto ToRestaurantDto(Restaurant r) => new()
    {
        Id        = r.Id,
        Name      = r.Name,
        Address   = r.Address,
        Latitude  = r.Latitude,
        Longitude = r.Longitude,
        Currency  = r.Currency,
        TimeZone  = r.TimeZone,
        IsActive  = r.IsActive,
        IsOpenNow = false,
        DistanceKm = null,
    };

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email)) return null;
        var at = email.IndexOf('@');
        if (at <= 1) return email;
        return email[0] + new string('*', at - 1) + email[at..];
    }

    private static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 7) return phone;
        return phone[..3] + new string('*', phone.Length - 6) + phone[^3..];
    }
}
