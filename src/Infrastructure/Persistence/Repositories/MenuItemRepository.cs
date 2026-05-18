using Core.Domain.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class MenuItemRepository(AppDbContext db) : IMenuItemRepository
{
    public Task<IReadOnlyList<MenuItem>> GetPagedAvailableAsync(Guid restaurantId, int page, int limit) =>
        db.MenuItems
            .Where(m => m.RestaurantId == restaurantId && m.IsActive && m.IsAvailable)
            .OrderBy(m => m.SortOrder)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<MenuItem>)t.Result);

    public Task<int> CountAvailableAsync(Guid restaurantId) =>
        db.MenuItems.CountAsync(m => m.RestaurantId == restaurantId && m.IsActive && m.IsAvailable);

    public Task<IReadOnlyList<MenuItem>> GetPagedAllAsync(Guid restaurantId, int page, int limit) =>
        db.MenuItems
            .Where(m => m.RestaurantId == restaurantId)
            .OrderBy(m => m.SortOrder)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<MenuItem>)t.Result);

    public Task<int> CountAllAsync(Guid restaurantId) =>
        db.MenuItems.CountAsync(m => m.RestaurantId == restaurantId);

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
