# Stripe Integration — Backend Guide

> **Scope:** This doc covers only the ASP.NET Core backend.
> React Native checkout and Admin Panel frontend are documented separately in Notion.

---

## Basket / Cart — do we persist it?

**No.** The basket lives entirely in client-side state (React Native Redux / Admin Panel local state). The backend never stores a basket. When the user taps "Pay", the client sends all selected items in a single `POST /orders/checkout` request and the backend validates, prices, and creates the order atomically.

**Why not persist it server-side:**
- Menu prices and availability can change — a saved basket goes stale
- Users complete orders in a single session; cross-session recovery is a V2 concern
- Server-side validation at checkout is the only reliable availability check anyway

**V2:** abandoned cart push notifications. At that point add a `BasketSnapshot` in Redis (TTL 24h), not PostgreSQL.

---

## Admin Panel — Recommended Functionality

Beyond the basic list + refund, these are worth building now because they share the same query layer:

| Feature | Route | Notes |
|---------|-------|-------|
| Order list with filters | `GET /admin/orders` | pagination, search, filter, sort |
| Order detail | `GET /admin/orders/{id}` | items, snapshot prices, Stripe intent ID |
| Refund | `POST /admin/orders/{id}/refund` | `Paid` orders only |
| Cancel | `POST /admin/orders/{id}/cancel` | `PendingPayment` orders only — abandoned checkouts |
| Stats widget | `GET /admin/orders/stats` | today's total + revenue + counts by status |

**Stats** are a single cheap query that drives a dashboard header row — total orders today, revenue today, pending/paid/cancelled/refunded counts. Build the endpoint now, it costs almost nothing alongside the list endpoint.

**Cancel** is needed for admin hygiene. `PendingPayment` orders pile up when users abandon the checkout sheet. Admins need a way to clean them up (or you can add a nightly job later — having the status transition already wired is required either way).

---

## Order Lifecycle

```
1. POST /orders/checkout
   → validate items + prices server-side
   → DB transaction: save Order (PendingPayment) + call Stripe + save IntentId
   → return { orderId, clientSecret }

2. Client presents Stripe payment sheet

3. Stripe fires: POST /webhooks/stripe
   payment_intent.succeeded  →  Order.Status = Paid
   payment_intent.failed     →  Order.Status = Cancelled
```

---

## Domain Models

### `OrderStatus` enum

```csharp
// Core/Domain/Enums/OrderStatus.cs
public enum OrderStatus
{
    PendingPayment,  // PaymentIntent created, awaiting payment
    Paid,            // Stripe webhook confirmed
    Cancelled,       // Payment failed, user abandoned, or admin cancelled
    Refunded,        // Stripe refund issued
}
```

### `Order` entity

```csharp
// Core/Domain/Entities/Order.cs
public class Order
{
    public Guid        Id                    { get; set; } = Guid.NewGuid();
    public string      UserId                { get; set; } = string.Empty;
    public Guid        RestaurantId          { get; set; }
    public string      Currency              { get; set; } = string.Empty;
    public decimal     Total                 { get; set; }
    public OrderStatus Status                { get; set; } = OrderStatus.PendingPayment;
    public string?     StripePaymentIntentId { get; set; }
    public DateTime    CreatedAt             { get; set; } = DateTime.UtcNow;

    public AppUser                User       { get; set; } = null!;
    public Restaurant             Restaurant { get; set; } = null!;
    public ICollection<OrderItem> Items      { get; set; } = [];
}
```

### `OrderItem` entity

Prices and names are **snapshotted at order time** — menu changes must not corrupt history.

```csharp
// Core/Domain/Entities/OrderItem.cs
public class OrderItem
{
    public Guid    Id         { get; set; } = Guid.NewGuid();
    public Guid    OrderId    { get; set; }
    public Guid    MenuItemId { get; set; }
    public string  Name       { get; set; } = string.Empty;  // snapshot
    public decimal UnitPrice  { get; set; }                  // snapshot
    public int     Quantity   { get; set; }
    public decimal LineTotal  => UnitPrice * Quantity;        // computed, not stored

    public Order    Order    { get; set; } = null!;
    public MenuItem MenuItem { get; set; } = null!;
}
```

### EF Core configuration

```csharp
// Infrastructure/Persistence/Configurations/OrderConfiguration.cs
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Total).HasPrecision(18, 2);
        builder.Property(o => o.Currency).IsRequired().HasMaxLength(3);
        builder.Property(o => o.StripePaymentIntentId).HasMaxLength(100);

        builder.HasOne(o => o.User)
               .WithMany()
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Restaurant)
               .WithMany()
               .HasForeignKey(o => o.RestaurantId)
               .OnDelete(DeleteBehavior.Restrict);

        // User history — filter by owner, sort by date
        builder.HasIndex(o => new { o.UserId, o.CreatedAt })
               .HasDatabaseName("ix_orders_userid_createdat");

        // Admin list — filter by restaurant + status, sort by date
        builder.HasIndex(o => new { o.RestaurantId, o.Status, o.CreatedAt })
               .HasDatabaseName("ix_orders_restaurantid_status_createdat");

        // Admin list — filter by status only (no restaurant filter)
        builder.HasIndex(o => new { o.Status, o.CreatedAt })
               .HasDatabaseName("ix_orders_status_createdat");

        // Webhook lookup by PaymentIntentId
        builder.HasIndex(o => o.StripePaymentIntentId)
               .HasDatabaseName("ix_orders_stripe_paymentintentid");
    }
}
```

```csharp
// Infrastructure/Persistence/Configurations/OrderItemConfiguration.cs
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.Ignore(i => i.LineTotal);

        builder.HasOne(i => i.Order)
               .WithMany(o => o.Items)
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.MenuItem)
               .WithMany()
               .HasForeignKey(i => i.MenuItemId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

Add to `AppDbContext`:
```csharp
public DbSet<Order>     Orders     => Set<Order>();
public DbSet<OrderItem> OrderItems => Set<OrderItem>();
```

Run the migration:
```bash
dotnet ef migrations add AddOrders --project src/Infrastructure --startup-project src/Api
```

---

## Environment Variables

```env
Stripe__PublishableKey=pk_test_...
Stripe__SecretKey=sk_test_...
Stripe__WebhookSecret=whsec_...
```

```csharp
// Infrastructure/Configuration/StripeOptions.cs
public class StripeOptions
{
    public string PublishableKey { get; init; } = string.Empty;
    public string SecretKey      { get; init; } = string.Empty;
    public string WebhookSecret  { get; init; } = string.Empty;
}
```

Register in `DependencyConfig.cs`:
```csharp
services.Configure<StripeOptions>(configuration.GetSection("Stripe"));
StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"]
    ?? throw new InvalidOperationException("Stripe:SecretKey is not configured");
```

| Key | Prefix | Used where |
|-----|--------|------------|
| Publishable (test) | `pk_test_` | Admin Panel always; RN dev |
| Secret (test) | `sk_test_` | Backend in development |
| Publishable (live) | `pk_live_` | React Native production only |
| Secret (live) | `sk_live_` | Backend in production only |
| Webhook secret | `whsec_` | Backend only — verifies signatures |

> Admin Panel **always** uses test keys, even in production.

---

## Package

```bash
dotnet add src/Infrastructure package Stripe.net
```

---

## DTOs

### Checkout

```csharp
// Core/DTOs/Orders/CheckoutRequest.cs
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
```

```csharp
// Core/DTOs/Orders/CheckoutResponse.cs
public record CheckoutResponse(Guid OrderId, string ClientSecret);
```

```csharp
// Core/DTOs/Orders/Validators/CheckoutRequestValidator.cs
public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item.");
        RuleForEach(x => x.Items).ChildRules(line =>
        {
            line.RuleFor(i => i.MenuItemId).NotEmpty();
            line.RuleFor(i => i.Quantity).InclusiveBetween(1, 50);
        });
    }
}
```

### Order response

```csharp
// Core/DTOs/Orders/OrderDto.cs
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
```

### Admin query — `OrderQuery`

```csharp
// Core/DTOs/Orders/OrderQuery.cs
public class OrderQuery
{
    public int          Page         { get; init; } = 1;
    public int          Limit        { get; init; } = 20;
    // Exact matches
    public Guid?        OrderId      { get; init; }
    public string?      UserId       { get; init; }
    public Guid?        RestaurantId { get; init; }
    // Range filters
    public OrderStatus? Status       { get; init; }
    public decimal?     MinTotal     { get; init; }
    public decimal?     MaxTotal     { get; init; }
    // Sort: "asc" | "desc" (default)
    public string       Sort         { get; init; } = "desc";
}
```

```csharp
// Core/DTOs/Orders/Validators/OrderQueryValidator.cs
public class OrderQueryValidator : AbstractValidator<OrderQuery>
{
    public OrderQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
        RuleFor(x => x.MinTotal).GreaterThanOrEqualTo(0).When(x => x.MinTotal.HasValue);
        RuleFor(x => x.MaxTotal).GreaterThanOrEqualTo(0).When(x => x.MaxTotal.HasValue);
        RuleFor(x => x).Must(x => x.MinTotal is null || x.MaxTotal is null || x.MinTotal <= x.MaxTotal)
            .WithMessage("MinTotal must be less than or equal to MaxTotal.");
        RuleFor(x => x.Sort).Must(s => s is "asc" or "desc")
            .WithMessage("Sort must be 'asc' or 'desc'.");
    }
}
```

### Client query — `MyOrderQuery`

```csharp
// Core/DTOs/Orders/MyOrderQuery.cs
public class MyOrderQuery
{
    public int          Page   { get; init; } = 1;
    public int          Limit  { get; init; } = 20;
    public OrderStatus? Status { get; init; }
}
```

### Admin stats

```csharp
// Core/DTOs/Orders/OrderStatsDto.cs
public record OrderStatsDto(
    int     TotalToday,
    decimal RevenueToday,
    int     PendingCount,
    int     PaidCount,
    int     CancelledCount,
    int     RefundedCount
);
```

### Refund request

```csharp
// Core/DTOs/Orders/RefundRequest.cs
public class RefundRequest
{
    public string? Reason { get; init; }
}
```

---

## IStripeService (Core layer)

Returns both `IntentId` and `ClientSecret` — `IntentId` is stored on the order at checkout time so refunds always have it, even before the webhook fires.

```csharp
// Core/Interfaces/IStripeService.cs
public interface IStripeService
{
    Task<(string IntentId, string ClientSecret)> CreatePaymentIntentAsync(
        long amountCents, string currency, Guid orderId);
    Task RefundAsync(string paymentIntentId, string? reason = null);
}
```

---

## StripeService (Infrastructure layer)

```csharp
// Infrastructure/Services/StripeService.cs
public class StripeService : IStripeService
{
    private readonly PaymentIntentService _intentService = new();
    private readonly RefundService        _refundService = new();

    public async Task<(string IntentId, string ClientSecret)> CreatePaymentIntentAsync(
        long amountCents, string currency, Guid orderId)
    {
        var intent = await _intentService.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount   = amountCents,
            Currency = currency.ToLower(),
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = orderId.ToString(),
            },
        });
        return (intent.Id, intent.ClientSecret);
    }

    public async Task RefundAsync(string paymentIntentId, string? reason = null)
    {
        await _refundService.CreateAsync(new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
            Reason        = reason ?? "requested_by_customer",
        });
    }
}
```

Register:
```csharp
services.AddScoped<IStripeService, StripeService>();
```

---

## IOrderService + IOrderRepository

```csharp
// Core/Interfaces/IOrderService.cs
public interface IOrderService
{
    Task<(CheckoutResponse? Result, string? Error)> CheckoutAsync(string userId, CheckoutRequest request);
    Task<PagedResult<OrderDto>>                      GetPagedAsync(OrderQuery query);          // admin
    Task<PagedResult<OrderDto>>                      GetMyOrdersAsync(string userId, MyOrderQuery query); // client
    Task<OrderDto?>                                  GetByIdAsync(Guid id);                    // admin
    Task<OrderDto?>                                  GetByIdAsync(Guid id, string userId);     // client — ownership check
    Task<OrderStatsDto>                              GetStatsAsync();                           // admin dashboard
    Task                                             MarkAsPaidAsync(Guid orderId, string paymentIntentId);
    Task                                             MarkAsFailedAsync(Guid orderId);
    Task                                             RefundAsync(Guid orderId, string? reason);
    Task<bool>                                       CancelAsync(Guid orderId);                // admin — PendingPayment only
}
```

```csharp
// Core/Interfaces/Repositories/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Order?>                                     GetByIdAsync(Guid id);
    Task<Order?>                                     GetByPaymentIntentIdAsync(string paymentIntentId);
    Task<Order>                                      AddAsync(Order order);
    Task                                             UpdateAsync(Order order);
    Task<(IReadOnlyList<Order> Items, int Total)>    GetPagedAsync(OrderQuery query);
    Task<(IReadOnlyList<Order> Items, int Total)>    GetByUserPagedAsync(string userId, MyOrderQuery query);
    Task<OrderStatsDto>                              GetStatsAsync(DateOnly date);
}
```

---

## OrderRepository

```csharp
// Infrastructure/Persistence/Repositories/OrderRepository.cs
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

        // Exact matches
        if (query.OrderId.HasValue)
            q = q.Where(o => o.Id == query.OrderId.Value);
        if (!string.IsNullOrWhiteSpace(query.UserId))
            q = q.Where(o => o.UserId == query.UserId);
        if (query.RestaurantId.HasValue)
            q = q.Where(o => o.RestaurantId == query.RestaurantId.Value);

        // Filters
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
```

---

## OrderService

Lives in `Infrastructure/Services/` because it depends on `IStripeService` and `AppDbContext`.

### Transaction strategy

`CheckoutAsync` wraps two writes in one transaction: order row + Stripe PaymentIntent. If Stripe throws, the transaction rolls back — no orphaned `PendingPayment` row in the DB. `ExecutionStrategy` handles transient Postgres failures with retries.

Webhook handlers are idempotent — Stripe delivers the same event more than once.

```csharp
// Infrastructure/Services/OrderService.cs
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
        // ── Validation ────────────────────────────────────────────────────────
        var restaurant = await restaurantRepository.GetByIdAsync(request.RestaurantId);
        if (restaurant is null || !restaurant.IsActive)
            return (null, "Restaurant not found or inactive.");

        var itemIds   = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await menuItemRepository.GetByIdsAsync(itemIds);

        var lines = new List<OrderItem>();
        foreach (var line in request.Items)
        {
            var item = menuItems.FirstOrDefault(m => m.Id == line.MenuItemId);
            if (item is null || !item.CanBeOrdered())
                return (null, $"Item {line.MenuItemId} is unavailable.");
            if (item.RestaurantId != request.RestaurantId)
                return (null, $"Item {line.MenuItemId} does not belong to this restaurant.");

            lines.Add(new OrderItem
            {
                MenuItemId = item.Id,
                Name       = item.Name,
                UnitPrice  = item.Price,
                Quantity   = line.Quantity,
            });
        }

        var total = lines.Sum(l => l.LineTotal);
        if (total <= 0)
            return (null, "Order total must be greater than zero.");

        // ── Transaction ───────────────────────────────────────────────────────
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();

            var order = new Order
            {
                UserId       = userId,
                RestaurantId = request.RestaurantId,
                Currency     = restaurant.Currency,
                Total        = total,
                Items        = lines,
            };

            await orderRepository.AddAsync(order);

            string intentId, clientSecret;
            try
            {
                (intentId, clientSecret) = await stripeService.CreatePaymentIntentAsync(
                    ToStripeCents(total, restaurant.Currency),
                    restaurant.Currency,
                    order.Id);
            }
            catch (StripeException ex)
            {
                await tx.RollbackAsync();
                logger.LogError(ex, "Stripe CreatePaymentIntent failed for Order {OrderId}", order.Id);
                return (null, "Payment service unavailable. Please try again.");
            }

            order.StripePaymentIntentId = intentId;
            await orderRepository.UpdateAsync(order);
            await tx.CommitAsync();

            logger.LogInformation("Order {OrderId} created — {Total} {Currency}",
                order.Id, total, restaurant.Currency);

            return (new CheckoutResponse(order.Id, clientSecret), null);
        });
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
        if (order.Status == OrderStatus.Paid) return;  // idempotent

        order.Status = OrderStatus.Paid;
        await orderRepository.UpdateAsync(order);
        logger.LogInformation("Order {OrderId} marked Paid", orderId);
    }

    public async Task MarkAsFailedAsync(Guid orderId)
    {
        var order = await orderRepository.GetByIdAsync(orderId);
        if (order is null) return;
        if (order.Status != OrderStatus.PendingPayment) return;  // idempotent

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

    // Stripe requires smallest currency unit. UZS is zero-decimal — no subunit.
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
```

> **Add `GetByIdsAsync` to `IMenuItemRepository` and `MenuItemRepository`:**
> ```csharp
> Task<IReadOnlyList<MenuItem>> GetByIdsAsync(IReadOnlyList<Guid> ids);
> // impl:
> public async Task<IReadOnlyList<MenuItem>> GetByIdsAsync(IReadOnlyList<Guid> ids) =>
>     await db.MenuItems.Where(m => ids.Contains(m.Id)).ToListAsync();
> ```

---

## Controllers

### Client — order history + checkout

```csharp
// Api/Controllers/OrdersController.cs
[ApiController]
[Route("orders")]
[Authorize]
[Tags("Orders")]
public class OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("checkout")]
    [ProducesResponseType<CheckoutResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var (result, error) = await orderService.CheckoutAsync(UserId, request);
        if (error is not null) return BadRequest(new { message = error });

        logger.LogInformation("Checkout initiated — Order {OrderId}", result!.OrderId);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<OrderDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders([FromQuery] MyOrderQuery query)
    {
        var result = await orderService.GetMyOrdersAsync(UserId, query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await orderService.GetByIdAsync(id, UserId);
        return order is null ? NotFound() : Ok(order);
    }
}
```

### Admin — order management

```csharp
// Api/Controllers/AdminOrdersController.cs
[ApiController]
[Route("admin/orders")]
[Authorize(Policy = TandurPolicies.AdminPanel)]
[Tags("Admin › Orders")]
public class AdminOrdersController(IOrderService orderService, ILogger<AdminOrdersController> logger)
    : ControllerBase
{
    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    [ProducesResponseType<PagedResult<OrderDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders([FromQuery] OrderQuery query)
    {
        var result = await orderService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("stats")]
    [ProducesResponseType<OrderStatsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var stats = await orderService.GetStatsAsync();
        return Ok(stats);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await orderService.GetByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest request)
    {
        var order = await orderService.GetByIdAsync(id);
        if (order is null) return NotFound();
        if (order.Status != nameof(OrderStatus.Paid))
            return BadRequest(new { message = "Only paid orders can be refunded." });

        await orderService.RefundAsync(id, request.Reason);
        logger.LogInformation("Order {OrderId} refunded by {ActorId}", id, ActorId);
        return Ok(new { message = "Refund issued." });
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var cancelled = await orderService.CancelAsync(id);
        if (!cancelled)
            return BadRequest(new { message = "Order not found or not in PendingPayment status." });

        logger.LogInformation("Order {OrderId} cancelled by {ActorId}", id, ActorId);
        return NoContent();
    }
}
```

### Webhook

```csharp
// Api/Controllers/StripeWebhookController.cs
[ApiController]
[Route("webhooks")]
[Tags("Webhooks")]
public class StripeWebhookController(
    IOrderService                    orderService,
    IOptions<StripeOptions>          stripeOptions,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost("stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> Stripe()
    {
        var json      = await new StreamReader(Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json, signature, stripeOptions.Value.WebhookSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning("Stripe webhook signature invalid: {Message}", ex.Message);
            return BadRequest();
        }

        if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
        {
            var intent  = (PaymentIntent)stripeEvent.Data.Object;
            var orderId = Guid.Parse(intent.Metadata["orderId"]);
            await orderService.MarkAsPaidAsync(orderId, intent.Id);
        }

        if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
        {
            var intent  = (PaymentIntent)stripeEvent.Data.Object;
            var orderId = Guid.Parse(intent.Metadata["orderId"]);
            await orderService.MarkAsFailedAsync(orderId);
        }

        return Ok();
    }
}
```

---

## API Routes Summary

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST | `/orders/checkout` | User JWT | Create order + PaymentIntent, return `clientSecret` |
| GET | `/orders` | User JWT | Own order history (paginated, filterable by status) |
| GET | `/orders/{id}` | User JWT | Own order detail |
| GET | `/admin/orders` | Admin JWT | All orders — search by orderId/userId/restaurantId, filter by status/total, sort by date |
| GET | `/admin/orders/stats` | Admin JWT | Today's totals + all-time counts by status |
| GET | `/admin/orders/{id}` | Admin JWT | Any order detail |
| POST | `/admin/orders/{id}/refund` | Admin JWT | Issue Stripe refund (`Paid` only) |
| POST | `/admin/orders/{id}/cancel` | Admin JWT | Cancel abandoned order (`PendingPayment` only) |
| POST | `/webhooks/stripe` | Signature only | `payment_intent.succeeded` / `.payment_failed` |

---

## Testing

### Stripe CLI (local webhook forwarding)

```bash
brew install stripe/stripe-cli/stripe
stripe login
stripe listen --forward-to https://localhost:5280/webhooks/stripe
# → prints whsec_test_... — add to .env as Stripe__WebhookSecret
```

### Trigger events manually

```bash
stripe trigger payment_intent.succeeded
stripe trigger payment_intent.payment_failed
```

### Test cards

| Card | Result |
|------|--------|
| `4242 4242 4242 4242` | Payment succeeds |
| `4000 0000 0000 0002` | Card declined |
| `4000 0025 0000 3155` | Requires 3D Secure |
| `4000 0000 0000 9995` | Insufficient funds |

Any future expiry, any 3-digit CVC, any postcode.

### Admin Panel test flow

The Admin Panel has a built-in **Test Checkout** page that calls `POST /orders/checkout` using Stripe Elements (test keys only). After payment, check `GET /admin/orders` and `GET /admin/orders/stats` to confirm the webhook fired and the order is marked Paid.

---

## Security checklist

- [ ] Webhook signature verified before any processing — never trust client callbacks
- [ ] Order only marked `Paid` via webhook, never from checkout response
- [ ] Prices computed server-side — client totals ignored
- [ ] `StripePaymentIntentId` indexed for fast webhook lookup
- [ ] `orderId` stored in PaymentIntent metadata for correlation
- [ ] `DB transaction` wraps order creation + Stripe call — rolls back on Stripe failure
- [ ] Webhook handlers are idempotent — safe for Stripe retries
- [ ] Secret key server-side only
- [ ] `whsec_` in environment variable, not source control
- [ ] Admin Panel always uses `pk_test_` / `sk_test_`
- [ ] Refund requires `Paid` status check before calling Stripe
- [ ] Cancel restricted to `PendingPayment` only
- [ ] Webhook endpoint is `[AllowAnonymous]`
