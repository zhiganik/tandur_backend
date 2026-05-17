namespace Core.DTOs.Auth;

public class AdminLoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
