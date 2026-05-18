namespace Core.Domain.Constants;

public static class OtpKeys
{
    public static string Phone(string phone)                         => $"phone:{phone}";
    public static string Email(string email)                         => $"email:{email}";
    public static string PhoneChange(string userId, string newPhone) => $"phone_change:{userId}:{newPhone}";
    public static string EmailChange(string userId, string newEmail) => $"email_change:{userId}:{newEmail}";
    public static string AccountDelete(string userId) => $"account_delete:{userId}";
}
