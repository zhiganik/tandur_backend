namespace Core.DTOs.Me;

public class VerifyPhoneChangeRequest
{
    public string NewPhone { get; init; } = string.Empty;
    public string Code     { get; init; } = string.Empty;
}
