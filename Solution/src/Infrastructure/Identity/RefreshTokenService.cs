using System;
using System.Threading.Tasks;
using Infrastructure.Data;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity;

public class RefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(ApplicationDbContext context, ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(string username, int daysValid)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var rec = new RefreshToken
        {
            Token = token,
            Username = username,
            ExpiresAt = DateTime.UtcNow.AddDays(daysValid),
            Revoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(rec);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created refresh token for user {Username} with id {TokenId} expiring at {Expires}", username, rec.Id, rec.ExpiresAt);

        return rec;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token, string username)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("ValidateRefreshToken called with empty token");
            return false;
        }

        var rec = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        if (rec is null)
        {
            _logger.LogWarning("Refresh token not found: {Token}", token);
            return false;
        }
        if (rec.Revoked)
        {
            _logger.LogWarning("Refresh token revoked for user {Username}", rec.Username);
            return false;
        }
        if (!string.Equals(rec.Username, username, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Refresh token username mismatch. Expected {Expected} actual {Actual}", rec.Username, username);
            return false;
        }
        if (rec.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token expired for user {Username}", rec.Username);
            return false;
        }
        _logger.LogInformation("Refresh token valid for user {Username}", rec.Username);
        return true;
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("RevokeRefreshToken called with empty token");
            return;
        }
        var rec = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        if (rec is null)
        {
            _logger.LogWarning("Attempt to revoke non-existent refresh token: {Token}", token);
            return;
        }
        rec.Revoked = true;
        _context.RefreshTokens.Update(rec);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked refresh token for user {Username} id {TokenId}", rec.Username, rec.Id);
    }

    public async Task<RefreshToken?> GetRecordAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("GetRecordAsync called with empty token");
            return null;
        }
        var rec = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        if (rec is null) _logger.LogDebug("No refresh token record found for token {Token}", token);
        else _logger.LogDebug("Retrieved refresh token record for user {Username} id {TokenId}", rec.Username, rec.Id);
        return rec;
    }
}
