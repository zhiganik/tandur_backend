namespace Core.DTOs.Auth;

public class SendPhoneOtpRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
}

public class VerifyPhoneOtpRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

public class SendEmailOtpRequest
{
    public string SessionToken { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public class VerifyEmailOtpRequest
{
    public string SessionToken { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

public class RegisterRequest
{
    public string SessionToken { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
}
