using Core.Domain.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public Task<IReadOnlyList<Category>> GetVisibleByRestaurantAsync(Guid restaurantId) =>
        db.Categories
            .Where(c => c.RestaurantId == restaurantId && c.IsVisible)
            .OrderBy(c => c.SortOrder)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Category>)t.Result);

    public Task<IReadOnlyList<Category>> GetAllByRestaurantAsync(Guid restaurantId) =>
        db.Categories
            .Where(c => c.RestaurantId == restaurantId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Category>)t.Result);

    public Task<Category?> GetByIdAsync(Guid id) =>
        db.Categories.FindAsync(id).AsTask()!;

    public async Task<Category> AddAsync(Category category)
    {
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    public Task UpdateAsync(Category category) => db.SaveChangesAsync();

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
