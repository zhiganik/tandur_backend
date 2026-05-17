using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ConsoleOtpSender(ILogger<ConsoleOtpSender> logger) : IOtpSender
{
    public Task SendSmsAsync(string phoneNumber, string code)
    {
        logger.LogInformation("[OTP-SMS] To: {Phone} | Code: {Code}", phoneNumber, code);
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string email, string code)
    {
        logger.LogInformation("[OTP-EMAIL] To: {Email} | Code: {Code}", email, code);
        return Task.CompletedTask;
    }
}