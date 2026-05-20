using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.DTOs.Common;
using Core.DTOs.Orders;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Infrastructure.Services;

public class OrderService(
    IOrderRepository      orderRepository,
    IMenuItemRepository   menuItemRepository,
    IRestaurantRepository restaurantRepository,
    IStripeService        stripeService,
    AppDbContext          db,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<(CheckoutResponse? Result, string? Error)> CheckoutAsync(
        string userId, CheckoutRequest request)
    {
        var restaurant = await restaurantRepository.GetByIdAsync(request.RestaurantId);
        if (restaurant is null || !restaurant.IsActive)
            return (null, "Restaurant not found or inactive.");

        var (lines, buildError) = await BuildOrderLinesAsync(request);
        if (buildError is not null)
            return (null, buildError);

        var total = lines!.Sum(l => l.UnitPrice * l.Quantity);
        if (total <= 0)
            return (null, "Order total must be greater than zero.");

        await using var tx = await db.Database.BeginTransactionAsync();

        var order = new Order
        {
            UserId       = userId,
            RestaurantId = restaurant.Id,
            Currency     = restaurant.Currency,
            Total        = total,
            Items        = lines,
        };

        await orderRepository.AddAsync(order);

        try
        {
            var (intentId, clientSecret) = await stripeService.CreatePaymentIntentAsync(
                ToStripeCents(total, restaurant.Currency), restaurant.Currency, order.Id);

            order.StripePaymentIntentId = intentId;
            await orderRepository.UpdateAsync(order);
            await tx.CommitAsync();

            logger.LogInformation("Order {OrderId} created — {Total} {Currency}",
                order.Id, total, restaurant.Currency);

            return (new CheckoutResponse(order.Id, clientSecret), null);
        }
        catch (StripeException ex)
        {
            await tx.RollbackAsync();
            logger.LogError(ex, "Stripe CreatePaymentIntent failed for Order {OrderId}", order.Id);
            return (null, "Payment service unavailable. Please try again.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<(List<OrderItem> Lines, string? Error)> BuildOrderLinesAsync(
        CheckoutRequest request)
    {
        var itemIds   = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await menuItemRepository.GetByIdsAsync(itemIds);

        var lines = new List<OrderItem>();
        foreach (var line in request.Items)
        {
            var item = menuItems.FirstOrDefault(m => m.Id == line.MenuItemId);
            if (item is null || !item.CanBeOrdered())
                return (lines, $"Item {line.MenuItemId} is unavailable.");
            if (item.RestaurantId != request.RestaurantId)
                return (lines, $"Item {line.MenuItemId} does not belong to this restaurant.");

            lines.Add(new OrderItem
            {
                MenuItemId = item.Id,
                Name       = item.Name,
                UnitPrice  = item.Price,
                Quantity   = line.Quantity,
            });
        }
        return (lines, null);
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(OrderQuery query)
    {
        var (items, total) = await orderRepository.GetPagedAsync(query);
        return new PagedResult<OrderDto>
        {
            Data  = items.Select(ToDto).ToList(),
            Total = total,
            Page  = query.Page,
            Limit = query.Limit,
        };
    }

    public async Task<PagedResult<OrderDto>> GetMyOrdersAsync(string userId, MyOrderQuery query)
    {
        var (items, total) = await orderRepository.GetByUserPagedAsync(userId, query);
        return new PagedResult<OrderDto>
        {
            Data  = items.Select(ToDto).ToList(),
            Total = total,
            Page  = query.Page,
            Limit = query.Limit,
        };
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        var order = await orderRepository.GetByIdAsync(id);
        return order is null ? null : ToDto(order);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, string userId)
    {
        var order = await orderRepository.GetByIdAsync(id);
        if (order is null || order.UserId != userId) return null;
        return ToDto(order);
    }

    public Task<OrderStatsDto> GetStatsAsync() =>
        orderRepository.GetStatsAsync(DateOnly.FromDateTime(DateTime.UtcNow));

    public async Task MarkAsPaidAsync(Guid orderId, string paymentIntentId)
    {
        var order = await orderRepository.GetByIdAsync(orderId);
        if (order is null)
        {
            logger.LogWarning("Webhook: order {OrderId} not found for PaymentIntent {IntentId}",
                orderId, paymentIntentId);
            return;
        }
        if (order.Status == OrderStatus.Paid) return;

        order.Status = OrderStatus.Paid;
        await orderRepository.UpdateAsync(order);
        logger.LogInformation("Order {OrderId} marked Paid", orderId);
    }

    public async Task MarkAsFailedAsync(Guid orderId)
    {
        var order = await orderRepository.GetByIdAsync(orderId);
        if (order is null) return;
        if (order.Status != OrderStatus.PendingPayment) return;

        order.Status = OrderStatus.Cancelled;
        await orderRepository.UpdateAsync(order);
        logger.LogWarning("Order {OrderId} cancelled — payment failed", orderId);
    }

    public async Task RefundAsync(Guid orderId, string? reason)
    {
        var order = await orderRepository.GetByIdAsync(orderId);
        if (order is null) return;
        if (order.StripePaymentIntentId is null)
        {
            logger.LogWarning("Refund requested for Order {OrderId} with no PaymentIntentId", orderId);
            return;
        }

        await stripeService.RefundAsync(order.StripePaymentIntentId, reason);
        order.Status = OrderStatus.Refunded;
        await orderRepository.UpdateAsync(order);
        logger.LogInformation("Order {OrderId} refunded", orderId);
    }

    public async Task<bool> CancelAsync(Guid orderId)
    {
        var order = await orderRepository.GetByIdAsync(orderId);
        if (order is null) return false;
        if (order.Status != OrderStatus.PendingPayment) return false;

        order.Status = OrderStatus.Cancelled;
        await orderRepository.UpdateAsync(order);
        logger.LogInformation("Order {OrderId} cancelled by admin", orderId);
        return true;
    }

    private static long ToStripeCents(decimal amount, string currency) =>
        currency.ToUpper() == "UZS"
            ? (long)Math.Round(amount)
            : (long)Math.Round(amount * 100);

    private static OrderDto ToDto(Order o) => new()
    {
        Id           = o.Id,
        RestaurantId = o.RestaurantId,
        Currency     = o.Currency,
        Total        = o.Total,
        Status       = o.Status.ToString(),
        CreatedAt    = o.CreatedAt,
        Items        = o.Items.Select(i => new OrderItemDto
        {
            MenuItemId = i.MenuItemId,
            Name       = i.Name,
            UnitPrice  = i.UnitPrice,
            Quantity   = i.Quantity,
            LineTotal  = i.LineTotal,
        }).ToList(),
    };
}
