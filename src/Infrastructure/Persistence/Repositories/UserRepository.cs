using Core.Domain.Entities;
using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db, UserManager<AppUser> userManager) : IUserRepository
{
    public Task<IReadOnlyList<AppUser>> GetPagedAsync(int page, int limit) =>
        db.Users
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<AppUser>)t.Result);

    public Task<int> CountAsync() => db.Users.CountAsync();

    public Task<AppUser?> GetByIdAsync(string id) =>
        userManager.FindByIdAsync(id)!;

    public async Task<Dictionary<string, IReadOnlyList<string>>> GetRolesMapAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.ToList();

        var rawRoles = await db.UserRoles
            .Where(ur => ids.Contains(ur.UserId))
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync();

        return ids.ToDictionary(
            id => id,
            id => (IReadOnlyList<string>)rawRoles
                .Where(r => r.UserId == id)
                .Select(r => r.Name!)
                .ToList()
        );
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return [];
        return (await userManager.GetRolesAsync(user)).ToList();
    }

    public async Task<(bool Success, string[] Errors)> UpdateAsync(AppUser user)
    {
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? (true, [])
            : (false, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<bool> DeleteAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return true;
    }

    public Task<AppUser?> GetByEmailAsync(string email) =>
        userManager.FindByEmailAsync(email)!;

    public Task<AppUser?> GetByPhoneAsync(string phone, string? excludeUserId = null) =>
        db.Users
            .Where(u => u.PhoneNumber == phone && (excludeUserId == null || u.Id != excludeUserId))
            .FirstOrDefaultAsync()!;

    public async Task<(bool Success, string[] Errors)> SetConfirmedPhoneAsync(string userId, string newPhone)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return (false, ["User not found."]);

        var setResult = await userManager.SetPhoneNumberAsync(user, newPhone);
        if (!setResult.Succeeded)
            return (false, setResult.Errors.Select(e => e.Description).ToArray());

        // SetPhoneNumberAsync resets PhoneNumberConfirmed — restore it
        user.PhoneNumberConfirmed = true;
        var updateResult = await userManager.UpdateAsync(user);
        return updateResult.Succeeded
            ? (true, [])
            : (false, updateResult.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool Success, string[] Errors)> SetConfirmedEmailAsync(string userId, string newEmail)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return (false, ["User not found."]);

        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var changeResult = await userManager.ChangeEmailAsync(user, newEmail, token);
        if (!changeResult.Succeeded)
            return (false, changeResult.Errors.Select(e => e.Description).ToArray());

        // Ensure EmailConfirmed is true after the OTP-verified change
        user.EmailConfirmed = true;
        var updateResult = await userManager.UpdateAsync(user);
        return updateResult.Succeeded
            ? (true, [])
            : (false, updateResult.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool Success, string[] Errors)> SetBirthdayAsync(string userId, DateTime? birthday)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return (false, ["User not found."]);

        user.DateOfBirth = birthday;
        return await UpdateAsync(user);
    }
}
