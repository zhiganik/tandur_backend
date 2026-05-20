using Core.DTOs.Common;
using Core.DTOs.Orders;

namespace Core.Interfaces;

public interface IOrderService
{
    Task<(CheckoutResponse? Result, string? Error)> CheckoutAsync(string userId, CheckoutRequest request);
    Task<PagedResult<OrderDto>>                      GetPagedAsync(OrderQuery query);
    Task<PagedResult<OrderDto>>                      GetMyOrdersAsync(string userId, MyOrderQuery query);
    Task<OrderDto?>                                  GetByIdAsync(Guid id);
    Task<OrderDto?>                                  GetByIdAsync(Guid id, string userId);
    Task<OrderStatsDto>                              GetStatsAsync();
    Task                                             MarkAsPaidAsync(Guid orderId, string paymentIntentId);
    Task                                             MarkAsFailedAsync(Guid orderId);
    Task                                             RefundAsync(Guid orderId, string? reason);
    Task<bool>                                       CancelAsync(Guid orderId);
}
