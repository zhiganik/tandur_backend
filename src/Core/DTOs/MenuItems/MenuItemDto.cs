namespace Core.DTOs.MenuItems;

public class MenuItemDto
{
    public Guid Id { get; init; }
    public Guid RestaurantId { get; init; }
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}
