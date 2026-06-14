using System;
using System.Threading.Tasks;
using Application.DTOs.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity;

public class AuthService
{
    private readonly JwtTokenGenerator _jwt;
    private readonly RefreshTokenService _refreshService;
    private readonly Microsoft.Extensions.Logging.ILogger<AuthService> _logger;

    public AuthService(JwtTokenGenerator jwt, RefreshTokenService refreshService, Microsoft.Extensions.Logging.ILogger<AuthService> logger)
    {
        _jwt = jwt;
        _refreshService = refreshService;
        _logger = logger;
    }

    public async Task<TokenResponseDto> LoginAsync(LoginRequestDto dto)
    {
        // For assignment purposes only: static validation
        _logger.LogInformation("Login attempt for user {Username}", dto.Username);
        if (!ValidateCredentials(dto.Username, dto.Password))
        {
            _logger.LogWarning("Invalid credentials for user {Username}", dto.Username);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // For this simple demo, map the Supervisor user to the Admin role.
        var roles = string.Equals(dto.Username, "Supervisor", StringComparison.OrdinalIgnoreCase)
            ? new[] { "Admin" }
            : Array.Empty<string>();

        var accessToken = _jwt.GenerateAccessToken(dto.Username, roles);
        var jwtSettings = _jwt.GetSettings();
        var refresh = await _refreshService.CreateRefreshTokenAsync(dto.Username, jwtSettings.RefreshTokenExpirationDays);

        var response = new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refresh.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddHours(jwtSettings.AccessTokenExpirationHours),
            RefreshTokenExpiresAt = refresh.ExpiresAt
        };

        _logger.LogInformation("User {Username} logged in successfully. Access token expires at {Expiry}", dto.Username, response.AccessTokenExpiresAt);
        return response;
    }

    public async Task<TokenResponseDto> RefreshAsync(RefreshRequestDto dto)
    {
        var rec = await _refreshService.GetRecordAsync(dto.RefreshToken);
        if (rec is null) throw new UnauthorizedAccessException("Invalid refresh token");
        if (!await _refreshService.ValidateRefreshTokenAsync(dto.RefreshToken, rec.Username)) throw new UnauthorizedAccessException("Invalid or expired refresh token");

        // rotate refresh token
        await _refreshService.RevokeRefreshTokenAsync(dto.RefreshToken);

        var jwtSettings = _jwt.GetSettings();
        var rolesForUser = string.Equals(rec.Username, "Supervisor", StringComparison.OrdinalIgnoreCase)
            ? new[] { "Admin" }
            : Array.Empty<string>();
        var newAccess = _jwt.GenerateAccessToken(rec.Username, rolesForUser);
        var newRefresh = await _refreshService.CreateRefreshTokenAsync(rec.Username, jwtSettings.RefreshTokenExpirationDays);

        var response = new TokenResponseDto
        {
            AccessToken = newAccess,
            RefreshToken = newRefresh.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddHours(jwtSettings.AccessTokenExpirationHours),
            RefreshTokenExpiresAt = newRefresh.ExpiresAt
        };

        return response;
    }

    public async Task LogoutAsync(RefreshRequestDto dto)
    {
        await _refreshService.RevokeRefreshTokenAsync(dto.RefreshToken);
        _logger.LogInformation("User logged out and refresh token revoked");
        await Task.CompletedTask;
    }

    private bool ValidateCredentials(string username, string password)
    {
        // Assignment credentials
        return string.Equals(username, "Supervisor", StringComparison.OrdinalIgnoreCase)
            && password == "Supervisor@123";
    }
}
