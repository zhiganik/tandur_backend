using Core.Domain.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RestaurantRepository(AppDbContext db) : IRestaurantRepository
{
    public Task<IReadOnlyList<Restaurant>> GetAllActiveAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.Restaurants
            .Where(r => r.IsActive)
            .Include(r => r.Schedules)
            .Include(r => r.Overrides.Where(o => o.Date == today))
            .OrderBy(r => r.CreatedAt)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Restaurant>)t.Result);
    }

    public Task<IReadOnlyList<Restaurant>> GetAllAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.Restaurants
            .Include(r => r.Schedules)
            .Include(r => r.Overrides.Where(o => o.Date == today))
            .OrderBy(r => r.CreatedAt)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Restaurant>)t.Result);
    }

    public Task<Restaurant?> GetByIdAsync(Guid id)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.Restaurants
            .Include(r => r.Schedules)
            .Include(r => r.Overrides.Where(o => o.Date == today))
            .FirstOrDefaultAsync(r => r.Id == id)!;
    }

    public async Task<Restaurant> AddAsync(Restaurant restaurant)
    {
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();
        return restaurant;
    }

    public Task UpdateAsync(Restaurant restaurant) => db.SaveChangesAsync();

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var restaurant = await db.Restaurants.FindAsync(id);
        if (restaurant is null) return false;

        restaurant.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }

    public Task<IReadOnlyList<Restaurant>> GetAllSummariesAsync() =>
        db.Restaurants
            .OrderBy(r => r.Name)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Restaurant>)t.Result);

    public async Task<Dictionary<string, IReadOnlyList<Restaurant>>> GetByAdminsAsync(IEnumerable<string> adminUserIds)
    {
        var ids  = adminUserIds.ToList();
        var rows = await db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, Restaurants = u.AssignedRestaurants.OrderBy(r => r.Name).ToList() })
            .ToListAsync();
        return rows.ToDictionary(x => x.Id, x => (IReadOnlyList<Restaurant>)x.Restaurants);
    }

    public Task<IReadOnlyList<Restaurant>> GetByAdminAsync(string adminUserId) =>
        db.Restaurants
            .Where(r => r.AssignedAdmins.Any(u => u.Id == adminUserId))
            .OrderBy(r => r.Name)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Restaurant>)t.Result);

    public async Task<bool> AssignToAdminAsync(Guid restaurantId, string adminUserId)
    {
        var restaurant = await db.Restaurants
            .Include(r => r.AssignedAdmins)
            .FirstOrDefaultAsync(r => r.Id == restaurantId);
        if (restaurant is null) return false;

        var admin = await db.Users.FindAsync(adminUserId);
        if (admin is null) return false;

        if (restaurant.AssignedAdmins.Any(u => u.Id == adminUserId)) return true;

        restaurant.AssignedAdmins.Add(admin);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnassignFromAdminAsync(Guid restaurantId, string adminUserId)
    {
        var restaurant = await db.Restaurants
            .Include(r => r.AssignedAdmins)
            .FirstOrDefaultAsync(r => r.Id == restaurantId);
        if (restaurant is null) return false;

        var admin = restaurant.AssignedAdmins.FirstOrDefault(u => u.Id == adminUserId);
        if (admin is null) return false;

        restaurant.AssignedAdmins.Remove(admin);
        await db.SaveChangesAsync();
        return true;
    }
}
