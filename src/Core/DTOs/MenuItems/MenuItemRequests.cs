namespace Core.DTOs.MenuItems;

public class CreateMenuItemRequest
{
    public Guid RestaurantId { get; init; }
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public bool IsAvailable { get; init; } = true;
}

public class UpdateMenuItemRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public Guid CategoryId { get; init; }
    public int SortOrder { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsActive { get; init; } = true;
}

public class PatchMenuItemRequest
{
    public bool? IsAvailable { get; init; }
    public bool? IsActive { get; init; }
    public decimal? Price { get; init; }
    public Guid? CategoryId { get; init; }
    public int? SortOrder { get; init; }
}
