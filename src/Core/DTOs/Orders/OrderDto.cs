namespace Core.DTOs.Orders;

public class OrderDto
{
    public Guid                        Id           { get; init; }
    public Guid                        RestaurantId { get; init; }
    public string                      Currency     { get; init; } = string.Empty;
    public decimal                     Total        { get; init; }
    public string                      Status       { get; init; } = string.Empty;
    public DateTime                    CreatedAt    { get; init; }
    public IReadOnlyList<OrderItemDto> Items        { get; init; } = [];
}

public class OrderItemDto
{
    public Guid    MenuItemId { get; init; }
    public string  Name      { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int     Quantity  { get; init; }
    public decimal LineTotal { get; init; }
}
