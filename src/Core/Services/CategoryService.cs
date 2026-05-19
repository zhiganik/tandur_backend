using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.DTOs.Categories;
using Core.Interfaces;
using Core.Interfaces.Repositories;

namespace Core.Services;

public class CategoryService(ICategoryRepository repository) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetVisibleByRestaurantAsync(Guid restaurantId)
    {
        var categories = await repository.GetVisibleByRestaurantAsync(restaurantId);
        return categories.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllByRestaurantAsync(Guid restaurantId)
    {
        var categories = await repository.GetAllByRestaurantAsync(restaurantId);
        return categories.Select(ToDto).ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id)
    {
        var category = await repository.GetByIdAsync(id);
        return category is null ? null : ToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(Guid restaurantId, CreateCategoryRequest request)
    {
        var maxOrder = await repository.GetMaxSortOrderAsync(restaurantId);
        var category = new Category
        {
            RestaurantId = restaurantId,
            Name         = request.Name,
            SortOrder    = maxOrder + 1,
            IsVisible    = request.IsVisible,
        };

        await repository.AddAsync(category);
        return ToDto(category);
    }

    public async Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        var category = await repository.GetByIdAsync(id);
        if (category is null) return null;

        category.Name      = request.Name;
        category.SortOrder = request.SortOrder;
        category.IsVisible = request.IsVisible;

        await repository.UpdateAsync(category);
        return ToDto(category);
    }

    public async Task<CategoryDto?> PatchAsync(Guid id, PatchCategoryRequest request)
    {
        var category = await repository.GetByIdAsync(id);
        if (category is null) return null;

        if (request.IsVisible.HasValue) category.IsVisible = request.IsVisible.Value;
        if (request.SortOrder.HasValue) category.SortOrder = request.SortOrder.Value;

        await repository.UpdateAsync(category);
        return ToDto(category);
    }

    public async Task<DeleteCategoryResult> DeleteAsync(Guid id)
    {
        var category = await repository.GetByIdAsync(id);
        if (category is null) return DeleteCategoryResult.NotFound;

        if (await repository.HasActiveItemsAsync(id)) return DeleteCategoryResult.HasItems;

        await repository.DeleteAsync(id);
        return DeleteCategoryResult.Deleted;
    }

    private static CategoryDto ToDto(Category c) => new()
    {
        Id           = c.Id,
        RestaurantId = c.RestaurantId,
        Name         = c.Name,
        SortOrder    = c.SortOrder,
        IsVisible    = c.IsVisible,
    };
}
