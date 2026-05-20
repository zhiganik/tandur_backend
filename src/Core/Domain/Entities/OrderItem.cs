namespace Core.Domain.Entities;

public class OrderItem
{
    public Guid    Id         { get; set; } = Guid.NewGuid();
    public Guid    OrderId    { get; set; }
    public Guid    MenuItemId { get; set; }
    public string  Name       { get; set; } = string.Empty;
    public decimal UnitPrice  { get; set; }
    public int     Quantity   { get; set; }
    public decimal LineTotal  => UnitPrice * Quantity;

    public Order    Order    { get; set; } = null!;
    public MenuItem MenuItem { get; set; } = null!;
}
