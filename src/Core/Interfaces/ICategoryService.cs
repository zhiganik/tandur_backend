using Core.Domain.Enums;
using Core.DTOs.Categories;
using Core.DTOs.Common;

namespace Core.Interfaces;

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> GetVisibleByRestaurantAsync(Guid restaurantId, PaginationQuery query);
    Task<PagedResult<CategoryDto>> GetAllByRestaurantAsync(Guid restaurantId, PaginationQuery query);
    Task<CategoryDto?>             GetByIdAsync(Guid id);
    Task<CategoryDto>              CreateAsync(Guid restaurantId, CreateCategoryRequest request);
    Task<CategoryDto?>             UpdateAsync(Guid id, UpdateCategoryRequest request);
    Task<CategoryDto?>             PatchAsync(Guid id, PatchCategoryRequest request);
    Task<DeleteCategoryResult>     DeleteAsync(Guid id);
}
