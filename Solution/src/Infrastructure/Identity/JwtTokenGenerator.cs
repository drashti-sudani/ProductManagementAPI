using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "DemoIssuer";
    public string Audience { get; set; } = "DemoAudience";
    public int AccessTokenExpirationHours { get; set; } = 24;
    public int RefreshTokenExpirationDays { get; set; } = 30;
}

public class JwtTokenGenerator
{
    private readonly JwtSettings _settings;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly SigningCredentials _signingCredentials;
    private readonly ILogger<JwtTokenGenerator> _logger;

    public JwtTokenGenerator(IConfiguration configuration, ILogger<JwtTokenGenerator> logger)
    {
        _logger = logger;
        _settings = configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
        // Ensure a non-empty secret is provided in configuration. Fail fast if missing to avoid using a weak default.
        if (string.IsNullOrWhiteSpace(_settings.Secret))
        {
            _logger.LogError("JWT secret is not configured. Set JwtSettings:Secret in appsettings or environment variables.");
            throw new InvalidOperationException("JWT secret is not configured. Set JwtSettings:Secret in configuration.");
        }

        // Derive a 256-bit key from the configured secret using SHA-256 so the key size meets HMAC-SHA256 requirements.
        // This allows configuration to use a passphrase while ensuring the signing key is the correct length.
        var secretBytes = Encoding.UTF8.GetBytes(_settings.Secret);
        var keyBytes = SHA256.HashData(secretBytes); // 32 bytes (256 bits)
        _signingKey = new SymmetricSecurityKey(keyBytes);
        _signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        _logger.LogDebug("JwtTokenGenerator initialized. AccessTokenExpiryHours={Hours}, RefreshTokenDays={Days}", _settings.AccessTokenExpirationHours, _settings.RefreshTokenExpirationDays);
    }

    public string GenerateAccessToken(string username, string[]? roles = null)
    {
        var creds = _signingCredentials;

        var claimsList = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, username)
        };

        if (roles is not null)
        {
            foreach (var role in roles)
            {
                claimsList.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var claims = claimsList.ToArray();

        var expires = DateTime.UtcNow.AddHours(_settings.AccessTokenExpirationHours);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds
        );
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        _logger.LogDebug("Generated access token for user {Username} expiring at {Expiry} (roles: {Roles})", username, expires, roles ?? Array.Empty<string>());
        return tokenString;
    }

    public JwtSettings GetSettings() => _settings;
}
