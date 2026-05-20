namespace Core.Interfaces;

public interface IStripeService
{
    Task<(string IntentId, string ClientSecret)> CreatePaymentIntentAsync(
        long amountCents, string currency, Guid orderId);
    Task RefundAsync(string paymentIntentId, string? reason = null);
}
