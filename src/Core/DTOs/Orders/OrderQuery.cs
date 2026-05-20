using Core.Domain.Enums;

namespace Core.DTOs.Orders;

public class OrderQuery
{
    public int          Page         { get; init; } = 1;
    public int          Limit        { get; init; } = 20;
    public Guid?        OrderId      { get; init; }
    public string?      UserId       { get; init; }
    public Guid?        RestaurantId { get; init; }
    public OrderStatus? Status       { get; init; }
    public decimal?     MinTotal     { get; init; }
    public decimal?     MaxTotal     { get; init; }
    public string       Sort         { get; init; } = "desc";
}
