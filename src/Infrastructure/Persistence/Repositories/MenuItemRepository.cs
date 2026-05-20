using Core.Domain.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class MenuItemRepository(AppDbContext db) : IMenuItemRepository
{
    public async Task<IReadOnlyList<MenuItem>> GetAllAvailableAsync(Guid restaurantId) =>
        await db.MenuItems
            .Where(m => m.RestaurantId == restaurantId && m.IsActive && m.IsAvailable)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

    public async Task<IReadOnlyList<MenuItem>> GetAllAsync(Guid restaurantId) =>
        await db.MenuItems
            .Where(m => m.RestaurantId == restaurantId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

    public async Task<int> GetMaxSortOrderAsync(Guid restaurantId)
    {
        var max = await db.MenuItems
            .Where(m => m.RestaurantId == restaurantId)
            .MaxAsync(m => (int?)m.SortOrder);
        return max ?? 0;
    }

    public Task<MenuItem?> GetByIdAsync(Guid id) =>
        db.MenuItems.FindAsync(id).AsTask()!;

    public async Task<MenuItem> AddAsync(MenuItem item)
    {
        db.MenuItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    public Task UpdateAsync(MenuItem item)
    {
        db.MenuItems.Update(item);
        return db.SaveChangesAsync();
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var item = await db.MenuItems.FindAsync(id);
        if (item is null) return false;

        item.IsActive = false;
        db.MenuItems.Update(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<MenuItem>> GetByIdsAsync(IReadOnlyList<Guid> ids) =>
        await db.MenuItems.Where(m => ids.Contains(m.Id)).ToListAsync();
}
