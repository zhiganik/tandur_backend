namespace Core.DTOs.Orders;

public record CheckoutResponse(Guid OrderId, string ClientSecret);
