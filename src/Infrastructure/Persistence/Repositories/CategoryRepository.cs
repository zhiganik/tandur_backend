using Core.Domain.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetVisibleByRestaurantAsync(Guid restaurantId) =>
        await db.Categories
            .Where(c => c.RestaurantId == restaurantId && c.IsVisible)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

    public async Task<IReadOnlyList<Category>> GetAllByRestaurantAsync(Guid restaurantId) =>
        await db.Categories
            .Where(c => c.RestaurantId == restaurantId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

    public async Task<int> GetMaxSortOrderAsync(Guid restaurantId)
    {
        var max = await db.Categories
            .Where(c => c.RestaurantId == restaurantId)
            .MaxAsync(c => (int?)c.SortOrder);
        return max ?? 0;
    }

    public Task<Category?> GetByIdAsync(Guid id) =>
        db.Categories.FindAsync(id).AsTask()!;

    public async Task<Category> AddAsync(Category category)
    {
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    public Task UpdateAsync(Category category)
    {
        db.Categories.Update(category);
        return db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return false;

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return true;
    }

    public Task<bool> HasActiveItemsAsync(Guid categoryId) =>
        db.MenuItems.AnyAsync(m => m.CategoryId == categoryId && m.IsActive);
}
