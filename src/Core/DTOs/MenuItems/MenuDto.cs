using Core.DTOs.Categories;

namespace Core.DTOs.MenuItems;

public class MenuDto
{
    public IReadOnlyList<CategoryDto> Categories { get; init; } = [];
    public IReadOnlyList<MenuItemDto> Items      { get; init; } = [];
}
