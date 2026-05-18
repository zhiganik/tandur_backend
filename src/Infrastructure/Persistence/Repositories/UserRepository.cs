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
}
