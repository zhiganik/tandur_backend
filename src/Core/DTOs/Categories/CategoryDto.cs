namespace Core.DTOs.Categories;

public class CategoryDto
{
    public Guid Id { get; init; }
    public Guid RestaurantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsVisible { get; init; }
}
