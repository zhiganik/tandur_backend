using Core.DTOs.Categories;
using Core.DTOs.Common;

namespace Core.DTOs.MenuItems;

public class MenuDto
{
    public IReadOnlyList<CategoryDto>  Categories { get; init; } = [];
    public PagedResult<MenuItemDto>    Items      { get; init; } = new();
}
