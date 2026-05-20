using Core.Domain.Entities;
using Core.DTOs.Users;
using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db, UserManager<AppUser> userManager) : IUserRepository
{
    public async Task<IReadOnlyList<AppUser>> GetPagedAsync(UserQuery query)
    {
        var q = db.Users.Include(u => u.AssignedRestaurants).AsQueryable();
        q = ApplyFilters(q, query);
        q = query.Sort == "asc"
            ? q.OrderBy(u => u.CreatedAt)
            : q.OrderByDescending(u => u.CreatedAt);
        return await q
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync();
    }

    public Task<int> CountAsync(UserQuery query) =>
        ApplyFilters(db.Users.AsQueryable(), query).CountAsync();

    private IQueryable<AppUser> ApplyFilters(IQueryable<AppUser> q, UserQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(u =>
                u.Id          == term ||
                u.Email       == term ||
                u.PhoneNumber == term ||
                u.FirstName.StartsWith(term) ||
                u.LastName.StartsWith(term));
        }

        if (query.Roles.Count > 0)
        {
            var userIdsWithRole = db.UserRoles
                .Join(db.Roles, ur => ur.RoleId, r => r.Id,
                      (ur, r) => new { ur.UserId, r.Name })
                .Where(x => query.Roles.Contains(x.Name!))
                .Select(x => x.UserId);

            q = q.Where(u => userIdsWithRole.Contains(u.Id));
        }

        if (query.RestaurantId.HasValue)
            q = q.Where(u => u.AssignedRestaurants.Any(r => r.Id == query.RestaurantId.Value));

        return q;
    }

    public Task<AppUser?> GetByIdAsync(string id) =>
        db.Users
            .Include(u => u.AssignedRestaurants)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<Dictionary<string, IReadOnlyList<string>>> GetRolesMapAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.ToList();
        if (ids.Count == 0) return [];

        var rawRoles = await db.UserRoles
            .Where(ur => ids.Contains(ur.UserId))
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync();

        var map = rawRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(x => x.Name!).ToList());

        foreach (var id in ids)
            map.TryAdd(id, []);

        return map;
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
