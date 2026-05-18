namespace Core.Domain.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    public Restaurant Restaurant { get; set; } = null!;
    public ICollection<MenuItem> Items { get; set; } = [];
}
