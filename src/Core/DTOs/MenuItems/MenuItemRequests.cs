namespace Core.DTOs.MenuItems;

public class CreateMenuItemRequest
{
    public Guid RestaurantId { get; init; }
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public bool IsAvailable { get; init; } = true;
    public int SortOrder { get; init; }
}

public class UpdateMenuItemRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public int SortOrder { get; init; }
    public bool IsAvailable { get; init; }
}

public class PatchMenuItemRequest
{
    public bool? IsAvailable { get; init; }
    public decimal? Price { get; init; }
    public Guid? CategoryId { get; init; }
    public int? SortOrder { get; init; }
}
