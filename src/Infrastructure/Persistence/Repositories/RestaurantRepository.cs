using Core.Domain.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RestaurantRepository(AppDbContext db) : IRestaurantRepository
{
    public Task<IReadOnlyList<Restaurant>> GetPagedActiveAsync(int page, int limit) =>
        db.Restaurants
            .Where(r => r.IsActive)
            .OrderBy(r => r.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Restaurant>)t.Result);

    public Task<int> CountActiveAsync() =>
        db.Restaurants.CountAsync(r => r.IsActive);

    public Task<IReadOnlyList<Restaurant>> GetPagedAllAsync(int page, int limit) =>
        db.Restaurants
            .OrderBy(r => r.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Restaurant>)t.Result);

    public Task<int> CountAllAsync() =>
        db.Restaurants.CountAsync();

    public Task<Restaurant?> GetByIdAsync(Guid id) =>
        db.Restaurants.FindAsync(id).AsTask()!;

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
