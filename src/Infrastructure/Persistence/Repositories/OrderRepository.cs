using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.DTOs.Orders;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id) =>
        db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

    public Task<Order?> GetByPaymentIntentIdAsync(string paymentIntentId) =>
        db.Orders.Include(o => o.Items)
          .FirstOrDefaultAsync(o => o.StripePaymentIntentId == paymentIntentId);

    public async Task<Order> AddAsync(Order order)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    public Task UpdateAsync(Order order)
    {
        db.Orders.Update(order);
        return db.SaveChangesAsync();
    }

    public async Task<(IReadOnlyList<Order> Items, int Total)> GetPagedAsync(OrderQuery query)
    {
        var q = db.Orders.Include(o => o.Items).AsQueryable();

        if (query.OrderId.HasValue)
            q = q.Where(o => o.Id == query.OrderId.Value);
        if (!string.IsNullOrWhiteSpace(query.UserId))
            q = q.Where(o => o.UserId == query.UserId);
        if (query.RestaurantId.HasValue)
            q = q.Where(o => o.RestaurantId == query.RestaurantId.Value);
        if (query.Status.HasValue)
            q = q.Where(o => o.Status == query.Status.Value);
        if (query.MinTotal.HasValue)
            q = q.Where(o => o.Total >= query.MinTotal.Value);
        if (query.MaxTotal.HasValue)
            q = q.Where(o => o.Total <= query.MaxTotal.Value);

        q = query.Sort == "asc"
            ? q.OrderBy(o => o.CreatedAt)
            : q.OrderByDescending(o => o.CreatedAt);

        var total = await q.CountAsync();
        var items = await q
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(IReadOnlyList<Order> Items, int Total)> GetByUserPagedAsync(
        string userId, MyOrderQuery query)
    {
        var q = db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId);

        if (query.Status.HasValue)
            q = q.Where(o => o.Status == query.Status.Value);

        q = q.OrderByDescending(o => o.CreatedAt);

        var total = await q.CountAsync();
        var items = await q
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<OrderStatsDto> GetStatsAsync(DateOnly date)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end   = start.AddDays(1);

        var todayRows = await db.Orders
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end)
            .Select(o => new { o.Status, o.Total })
            .ToListAsync();

        var allCounts = await db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int Count(OrderStatus s) =>
            allCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        return new OrderStatsDto(
            TotalToday    : todayRows.Count,
            RevenueToday  : todayRows.Where(o => o.Status == OrderStatus.Paid).Sum(o => o.Total),
            PendingCount  : Count(OrderStatus.PendingPayment),
            PaidCount     : Count(OrderStatus.Paid),
            CancelledCount: Count(OrderStatus.Cancelled),
            RefundedCount : Count(OrderStatus.Refunded)
        );
    }
}
