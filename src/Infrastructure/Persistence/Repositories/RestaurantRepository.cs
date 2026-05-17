using Core.Domain.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RestaurantRepository(AppDbContext db) : IRestaurantRepository
{
    public Task<IReadOnlyList<Restaurant>> GetActiveAsync() =>
        db.Restaurants.Where(r => r.IsActive).ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Restaurant>)t.Result);

    public Task<IReadOnlyList<Restaurant>> GetAllAsync() =>
        db.Restaurants.OrderBy(r => r.CreatedAt).ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Restaurant>)t.Result);

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
}
