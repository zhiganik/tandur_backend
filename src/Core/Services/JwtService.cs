using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Core.Services;

public class JwtService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtService(IConfiguration config)
    {
        _issuer = config["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");

        _audience = config["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

        if (!int.TryParse(config["Jwt:ExpiryMinutes"], out _expiryMinutes) || _expiryMinutes <= 0)
            throw new InvalidOperationException("Jwt:ExpiryMinutes must be a positive integer.");

        var rawKey = config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        if (Encoding.UTF8.GetByteCount(rawKey) < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes (256 bits) for HMAC-SHA256.");

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
    }

    public DateTime GetExpiry() => DateTime.UtcNow.AddMinutes(_expiryMinutes);

    public string GenerateToken(AppUser user, IList<string> roles, IEnumerable<Claim>? extraClaims = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? throw new InvalidOperationException("User has no email.")),
            new(ClaimTypes.Name, user.UserName ?? user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (extraClaims is not null)
            claims.AddRange(extraClaims);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: GetExpiry(),
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
