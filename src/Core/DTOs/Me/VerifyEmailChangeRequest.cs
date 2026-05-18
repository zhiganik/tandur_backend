namespace Core.DTOs.Me;

public class VerifyEmailChangeRequest
{
    public string NewEmail { get; init; } = string.Empty;
    public string Code     { get; init; } = string.Empty;
}
