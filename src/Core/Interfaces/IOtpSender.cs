namespace Core.Interfaces;

public interface IOtpSender
{
    Task SendSmsAsync(string phoneNumber, string code);
    Task SendEmailAsync(string email, string code);
}
