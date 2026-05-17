namespace Core.DTOs.Auth;

public class ChangePasswordRequest
{
    public string NewPassword { get; init; } = string.Empty;
}
