using Core.Interfaces;
using Stripe;

namespace Infrastructure.Services;

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
