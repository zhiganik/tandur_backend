using Core.Domain.Entities;
using Core.DTOs.Categories;
using Core.DTOs.MenuItems;
using Core.Interfaces;
using Core.Interfaces.Repositories;

namespace Core.Services;

public class MenuItemService(IMenuItemRepository menuItemRepo, ICategoryRepository categoryRepo, IStorageService storageService) : IMenuItemService
{
    public async Task<MenuDto> GetMenuAsync(Guid restaurantId)
    {
        var categories = await categoryRepo.GetVisibleByRestaurantAsync(restaurantId);
        var items      = await menuItemRepo.GetAllAvailableAsync(restaurantId);

        return new MenuDto
        {
            Categories = categories.Select(ToCategoryDto).ToList(),
            Items      = items.Select(ToMenuItemDto).ToList(),
        };
    }

    public async Task<MenuDto> GetAdminMenuAsync(Guid restaurantId)
    {
        var categories = await categoryRepo.GetAllByRestaurantAsync(restaurantId);
        var items      = await menuItemRepo.GetAllAsync(restaurantId);

        return new MenuDto
        {
            Categories = categories.Select(ToCategoryDto).ToList(),
            Items      = items.Select(ToMenuItemDto).ToList(),
        };
    }

    public async Task<MenuItemDto?> GetByIdAsync(Guid id)
    {
        var item = await menuItemRepo.GetByIdAsync(id);
        return item is null || !item.IsActive ? null : ToMenuItemDto(item);
    }

    public async Task<MenuItemDto?> GetAdminByIdAsync(Guid id)
    {
        var item = await menuItemRepo.GetByIdAsync(id);
        return item is null ? null : ToMenuItemDto(item);
    }

    public async Task<MenuItemDto> CreateAsync(CreateMenuItemRequest request)
    {
        var maxOrder = await menuItemRepo.GetMaxSortOrderAsync(request.RestaurantId);
        var item = new MenuItem
        {
            RestaurantId     = request.RestaurantId,
            CategoryId       = request.CategoryId,
            Name             = request.Name,
            Description      = request.Description,
            ShortDescription = request.ShortDescription,
            Price            = request.Price,
            IsAvailable      = request.IsAvailable,
            SortOrder        = maxOrder + 1,
        };

        await menuItemRepo.AddAsync(item);
        return ToMenuItemDto(item);
    }

    public async Task<MenuItemDto?> UpdateAsync(Guid id, UpdateMenuItemRequest request)
    {
        var item = await menuItemRepo.GetByIdAsync(id);
        if (item is null) return null;

        item.Name             = request.Name;
        item.Description      = request.Description;
        item.ShortDescription = request.ShortDescription;
        item.Price            = request.Price;
        item.CategoryId       = request.CategoryId;
        item.SortOrder        = request.SortOrder;
        item.IsAvailable      = request.IsAvailable;
        item.IsActive         = request.IsActive;

        await menuItemRepo.UpdateAsync(item);
        return ToMenuItemDto(item);
    }

    public async Task<MenuItemDto?> PatchAsync(Guid id, PatchMenuItemRequest request)
    {
        var item = await menuItemRepo.GetByIdAsync(id);
        if (item is null) return null;

        if (request.IsAvailable.HasValue) item.IsAvailable = request.IsAvailable.Value;
        if (request.IsActive.HasValue)    item.IsActive    = request.IsActive.Value;
        if (request.Price.HasValue)       item.Price       = request.Price.Value;
        if (request.CategoryId.HasValue)  item.CategoryId  = request.CategoryId.Value;
        if (request.SortOrder.HasValue)   item.SortOrder   = request.SortOrder.Value;

        await menuItemRepo.UpdateAsync(item);
        return ToMenuItemDto(item);
    }

    public Task<bool> DeleteAsync(Guid id) => menuItemRepo.SoftDeleteAsync(id);

    public async Task<ImageUploadResult?> UploadImageAsync(Guid id, Stream stream, string contentType, string fileName)
    {
        var item = await menuItemRepo.GetByIdAsync(id);
        if (item is null) return null;

        if (item.ImageUrl is not null)
            await storageService.DeleteAsync(storageService.ExtractKey(item.ImageUrl));

        var ext = contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png"  => "png",
            "image/webp" => "webp",
            _            => "jpg",
        };
        var key      = $"menu-items/{id}/{Guid.NewGuid()}.{ext}";
        var imageUrl = await storageService.UploadAsync(key, stream, contentType);

        item.ImageUrl = imageUrl;
        await menuItemRepo.UpdateAsync(item);

        return new ImageUploadResult { ImageUrl = imageUrl };
    }

    public async Task<bool> DeleteImageAsync(Guid id)
    {
        var item = await menuItemRepo.GetByIdAsync(id);
        if (item is null) return false;

        if (item.ImageUrl is not null)
        {
            await storageService.DeleteAsync(storageService.ExtractKey(item.ImageUrl));
            item.ImageUrl = null;
            await menuItemRepo.UpdateAsync(item);
        }

        return true;
    }

    private static CategoryDto ToCategoryDto(Category c) => new()
    {
        Id           = c.Id,
        RestaurantId = c.RestaurantId,
        Name         = c.Name,
        SortOrder    = c.SortOrder,
        IsVisible    = c.IsVisible,
    };

    private static MenuItemDto ToMenuItemDto(MenuItem m) => new()
    {
        Id               = m.Id,
        RestaurantId     = m.RestaurantId,
        CategoryId       = m.CategoryId,
        Name             = m.Name,
        Description      = m.Description,
        ShortDescription = m.ShortDescription,
        Price            = m.Price,
        ImageUrl         = m.ImageUrl,
        IsAvailable      = m.IsAvailable,
        IsActive         = m.IsActive,
        SortOrder        = m.SortOrder,
    };
}
