using Core.DTOs.MenuItems;

namespace Core.Interfaces;

public interface IMenuItemService
{
    Task<MenuDto>      GetMenuAsync(Guid restaurantId);
    Task<MenuDto>      GetAdminMenuAsync(Guid restaurantId);
    Task<MenuItemDto?> GetByIdAsync(Guid id);
    Task<MenuItemDto?> GetAdminByIdAsync(Guid id);
    Task<MenuItemDto>  CreateAsync(CreateMenuItemRequest request);
    Task<MenuItemDto?> UpdateAsync(Guid id, UpdateMenuItemRequest request);
    Task<MenuItemDto?> PatchAsync(Guid id, PatchMenuItemRequest request);
    Task<bool>               DeleteAsync(Guid id);
    Task<ImageUploadResult?> UploadImageAsync(Guid id, Stream stream, string contentType, string fileName);
    Task<bool>               DeleteImageAsync(Guid id);
}
