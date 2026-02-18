using Oikos.Application.Data;
using Oikos.Application.Services.Security;
using Oikos.Domain.Entities.Rbac;
using Oikos.Application.Services.Email;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Oikos.Application.Services.Authentication;

public class PasswordResetService
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<PasswordResetService> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly EmailOptions _emailOptions;

    public PasswordResetService(
        IAppDbContextFactory dbFactory,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        ILogger<PasswordResetService> logger,
        IPasswordHasher passwordHasher)
    {
        _dbFactory = dbFactory;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task RequestPasswordResetAsync(string identifier, string applicationBaseUri, bool isBonixSource = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return;
        }

        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var trimmedIdentifier = identifier.Trim();
        var normalizedIdentifier = trimmedIdentifier.ToLowerInvariant();
        var user = await context.Users
            .Where(u =>
                (u.Email != null && u.Email.ToLower() == normalizedIdentifier) ||
                (u.Name != null && u.Name.ToLower() == normalizedIdentifier))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var expiryMinutes = _emailOptions.ResetTokenExpirationMinutes > 0
            ? _emailOptions.ResetTokenExpirationMinutes
            : 60;

        var now = DateTime.UtcNow;
        var staleTokens = await context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && (t.ExpiresAt < now || t.ConsumedAt != null))
            .ToListAsync(cancellationToken);
        if (staleTokens.Count > 0)
        {
            context.PasswordResetTokens.RemoveRange(staleTokens);
        }

        var expiresAt = now.AddMinutes(expiryMinutes);
        var tokenBytes = RandomNumberGenerator.GetBytes(48);
        var rawToken = WebEncoders.Base64UrlEncode(tokenBytes);
        var tokenHash = _passwordHasher.HashPassword(rawToken);

        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = expiresAt,
        });

        await context.SaveChangesAsync(cancellationToken);

        var baseUri = applicationBaseUri.EndsWith('/') ? applicationBaseUri : applicationBaseUri + "/";
        var resetLink = new Uri(new Uri(baseUri), $"reset-password/{rawToken}").ToString();

        // Use EmailBonix configuration if request came from Bonix pages
        var emailConfig = isBonixSource ? EmailConfigurationType.Bonix : EmailConfigurationType.Default;

        await _emailSender.SendPasswordResetEmailAsync(user.Email!, user.RealName, resetLink, expiresAt, emailConfig, cancellationToken);
        _logger.LogInformation("Issued password reset token for user {UserId}", user.Id);
    }

    public async Task<bool> IsResetTokenValidAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var tokens = await context.PasswordResetTokens
            .Where(t => t.ConsumedAt == null && t.ExpiresAt >= now)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return tokens.Any(t => _passwordHasher.VerifyPassword(t.TokenHash, token));
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var tokens = await context.PasswordResetTokens
            .Where(t => t.ConsumedAt == null && t.ExpiresAt >= now)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var resetToken in tokens)
        {
            if (!_passwordHasher.VerifyPassword(resetToken.TokenHash, token))
            {
                continue;
            }

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == resetToken.UserId, cancellationToken);
            if (user is null)
            {
                resetToken.ConsumedAt = now;
                await context.SaveChangesAsync(cancellationToken);
                return false;
            }

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            resetToken.ConsumedAt = now;
            foreach (var otherToken in tokens.Where(t => t.UserId == resetToken.UserId && t.Id != resetToken.Id))
            {
                otherToken.ConsumedAt = now;
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Password reset successfully completed for user {UserId}", user.Id);
            return true;
        }

        return false;
    }
}
