using Core.Domain.Enums;

namespace Core.DTOs.Orders;

public class MyOrderQuery
{
    public int          Page   { get; init; } = 1;
    public int          Limit  { get; init; } = 20;
    public OrderStatus? Status { get; init; }
}
