namespace Core.DTOs.Orders;

public class CheckoutRequest
{
    public Guid                        RestaurantId { get; init; }
    public IReadOnlyList<CheckoutLine> Items        { get; init; } = [];
}

public class CheckoutLine
{
    public Guid MenuItemId { get; init; }
    public int  Quantity   { get; init; }
}
