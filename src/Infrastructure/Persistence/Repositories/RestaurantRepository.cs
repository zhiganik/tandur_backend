using Core.Domain.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RestaurantRepository(AppDbContext db) : IRestaurantRepository
{
    public async Task<IReadOnlyList<Restaurant>> GetAllActiveAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.Restaurants
            .Where(r => r.IsActive)
            .Include(r => r.Schedules)
            .Include(r => r.Overrides.Where(o => o.Date == today))
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Restaurant>> GetAllAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.Restaurants
            .Include(r => r.Schedules)
            .Include(r => r.Overrides.Where(o => o.Date == today))
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public Task<Restaurant?> GetByIdAsync(Guid id)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.Restaurants
            .Include(r => r.Schedules)
            .Include(r => r.Overrides.Where(o => o.Date == today))
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Restaurant> AddAsync(Restaurant restaurant)
    {
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();
        return restaurant;
    }

    public Task UpdateAsync(Restaurant restaurant)
    {
        db.Restaurants.Update(restaurant);
        return db.SaveChangesAsync();
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var restaurant = await db.Restaurants.FindAsync(id);
        if (restaurant is null) return false;

        restaurant.IsActive = false;
        db.Restaurants.Update(restaurant);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<Restaurant>> GetByAdminAsync(string adminUserId) =>
        await db.Restaurants
            .Where(r => r.AssignedAdmins.Any(u => u.Id == adminUserId))
            .OrderBy(r => r.Name)
            .ToListAsync();

    public async Task<bool> AssignToAdminAsync(Guid restaurantId, string adminUserId)
    {
        var restaurant = await db.Restaurants
            .Include(r => r.AssignedAdmins)
            .AsTracking()
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
            .AsTracking()
            .FirstOrDefaultAsync(r => r.Id == restaurantId);
        if (restaurant is null) return false;

        var admin = restaurant.AssignedAdmins.FirstOrDefault(u => u.Id == adminUserId);
        if (admin is null) return false;

        restaurant.AssignedAdmins.Remove(admin);
        await db.SaveChangesAsync();
        return true;
    }
}
