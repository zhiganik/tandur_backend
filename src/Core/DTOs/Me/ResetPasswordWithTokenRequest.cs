namespace Core.DTOs.Me;

public class ResetPasswordWithTokenRequest
{
    public string Token       { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
