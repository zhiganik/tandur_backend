using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ConsolePasswordResetSender(ILogger<ConsolePasswordResetSender> logger) : IPasswordResetSender
{
    public Task SendAsync(string email, string resetToken)
    {
        // TODO: replace with real email link when email service is wired up
        logger.LogInformation("[DEV] Password reset token for {Email}: {Token}", email, resetToken);
        return Task.CompletedTask;
    }
}
