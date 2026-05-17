namespace Core.DTOs.Auth;

public class TokenResponse
{
    public TokenResponse(string accessToken, string refreshToken, DateTime expiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
    }

    public string AccessToken { get; }
    public string RefreshToken { get; }
    public DateTime ExpiresAt { get; }
}
