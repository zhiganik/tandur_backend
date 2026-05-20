using Core.Domain.Entities;
using Core.DTOs.Orders;

namespace Core.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<Order?>                                  GetByIdAsync(Guid id);
    Task<Order?>                                  GetByPaymentIntentIdAsync(string paymentIntentId);
    Task<Order>                                   AddAsync(Order order);
    Task                                          UpdateAsync(Order order);
    Task<(IReadOnlyList<Order> Items, int Total)> GetPagedAsync(OrderQuery query);
    Task<(IReadOnlyList<Order> Items, int Total)> GetByUserPagedAsync(string userId, MyOrderQuery query);
    Task<OrderStatsDto>                           GetStatsAsync(DateOnly date);
}
