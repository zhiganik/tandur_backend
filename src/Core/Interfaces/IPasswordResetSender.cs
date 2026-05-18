namespace Core.Interfaces;

public interface IPasswordResetSender
{
    Task SendAsync(string email, string resetToken);
}
