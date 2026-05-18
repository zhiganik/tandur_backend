using Core.Domain.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class MenuItemRepository(AppDbContext db) : IMenuItemRepository
{
    public Task<IReadOnlyList<MenuItem>> GetAvailableByRestaurantAsync(Guid restaurantId) =>
        db.MenuItems
            .Where(m => m.RestaurantId == restaurantId && m.IsActive && m.IsAvailable)
            .OrderBy(m => m.SortOrder)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<MenuItem>)t.Result);

    public Task<IReadOnlyList<MenuItem>> GetAllByRestaurantAsync(Guid restaurantId) =>
        db.MenuItems
            .Where(m => m.RestaurantId == restaurantId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<MenuItem>)t.Result);

    public Task<MenuItem?> GetByIdAsync(Guid id) =>
        db.MenuItems.FindAsync(id).AsTask()!;

    public async Task<MenuItem> AddAsync(MenuItem item)
    {
        db.MenuItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    public Task UpdateAsync(MenuItem item) => db.SaveChangesAsync();

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var item = await db.MenuItems.FindAsync(id);
        if (item is null) return false;

        item.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }
}
