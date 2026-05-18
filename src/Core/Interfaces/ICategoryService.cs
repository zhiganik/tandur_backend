using Core.Domain.Enums;
using Core.DTOs.Categories;

namespace Core.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetVisibleByRestaurantAsync(Guid restaurantId);
    Task<IReadOnlyList<CategoryDto>> GetAllByRestaurantAsync(Guid restaurantId);
    Task<CategoryDto?> GetByIdAsync(Guid id);
    Task<CategoryDto> CreateAsync(Guid restaurantId, CreateCategoryRequest request);
    Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryRequest request);
    Task<CategoryDto?> PatchAsync(Guid id, PatchCategoryRequest request);
    Task<DeleteCategoryResult> DeleteAsync(Guid id);
}
