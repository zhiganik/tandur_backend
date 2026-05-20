using Core.Domain.Enums;

namespace Core.Domain.Entities;

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
