namespace Core.DTOs.Auth;

public class ChangePasswordWithOtpRequest
{
    public string Code        { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
