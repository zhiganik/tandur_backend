using Core.Domain.Entities;
using Core.DTOs.Categories;
using Core.DTOs.Common;
using Core.DTOs.MenuItems;
using Core.Interfaces;
using Core.Interfaces.Repositories;

namespace Core.Services;

public class MenuItemService(IMenuItemRepository menuItemRepo, ICategoryRepository categoryRepo) : IMenuItemService
{
    public async Task<MenuDto> GetMenuAsync(Guid restaurantId, PaginationQuery query)
    {
        var categories = await categoryRepo.GetVisibleByRestaurantAsync(restaurantId);
        var total      = await menuItemRepo.CountAvailableAsync(restaurantId);
        var items      = await menuItemRepo.GetPagedAvailableAsync(restaurantId, query.Page, query.Limit);

        return new MenuDto
        {
            Categories = categories.Select(ToCategoryDto).ToList(),
            Items      = new PagedResult<MenuItemDto>
            {
                Data  = items.Select(ToMenuItemDto).ToList(),
                Total = total,
                Page  = query.Page,
                Limit = query.Limit,
            },
        };
    }

    public async Task<MenuDto> GetAdminMenuAsync(Guid restaurantId, PaginationQuery query)
    {
        var categories = await categoryRepo.GetAllByRestaurantAsync(restaurantId);
        var total      = await menuItemRepo.CountAllAsync(restaurantId);
        var items      = await menuItemRepo.GetPagedAllAsync(restaurantId, query.Page, query.Limit);

        return new MenuDto
        {
            Categories = categories.Select(ToCategoryDto).ToList(),
            Items      = new PagedResult<MenuItemDto>
            {
                Data  = items.Select(ToMenuItemDto).ToList(),
                Total = total,
                Page  = query.Page,
                Limit = query.Limit,
            },
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
        var item = new MenuItem
        {
            RestaurantId     = request.RestaurantId,
            CategoryId       = request.CategoryId,
            Name             = request.Name,
            Description      = request.Description,
            ShortDescription = request.ShortDescription,
            Price            = request.Price,
            Currency         = request.Currency,
            IsAvailable      = request.IsAvailable,
            SortOrder        = request.SortOrder,
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
        item.Currency         = request.Currency;
        item.CategoryId       = request.CategoryId;
        item.SortOrder        = request.SortOrder;
        item.IsAvailable      = request.IsAvailable;

        await menuItemRepo.UpdateAsync(item);
        return ToMenuItemDto(item);
    }

    public async Task<MenuItemDto?> PatchAsync(Guid id, PatchMenuItemRequest request)
    {
        var item = await menuItemRepo.GetByIdAsync(id);
        if (item is null) return null;

        if (request.IsAvailable.HasValue) item.IsAvailable = request.IsAvailable.Value;
        if (request.Price.HasValue)       item.Price       = request.Price.Value;
        if (request.CategoryId.HasValue)  item.CategoryId  = request.CategoryId.Value;
        if (request.SortOrder.HasValue)   item.SortOrder   = request.SortOrder.Value;

        await menuItemRepo.UpdateAsync(item);
        return ToMenuItemDto(item);
    }

    public Task<bool> DeleteAsync(Guid id) => menuItemRepo.SoftDeleteAsync(id);

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
        Currency         = m.Currency,
        ImageUrl         = m.ImageUrl,
        IsAvailable      = m.IsAvailable,
        IsActive         = m.IsActive,
        SortOrder        = m.SortOrder,
    };
}
