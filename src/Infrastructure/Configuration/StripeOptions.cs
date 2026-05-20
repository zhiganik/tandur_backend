namespace Infrastructure.Configuration;

public class StripeOptions
{
    public string PublishableKey { get; init; } = string.Empty;
    public string SecretKey      { get; init; } = string.Empty;
    public string WebhookSecret  { get; init; } = string.Empty;
}
