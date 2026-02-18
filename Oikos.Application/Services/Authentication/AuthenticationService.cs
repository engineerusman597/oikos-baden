using Oikos.Domain.Constants;
using Oikos.Common.Constants;
using Oikos.Domain.Entities.Log;
using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Application.Services.Authentication.Models;
using Oikos.Application.Services.Security;
using Oikos.Application.Extensions;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Oikos.Application.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public AuthenticationService(
        IAppDbContextFactory dbFactory, 
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _dbFactory = dbFactory;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var normalizedIdentifier = request.Identifier.Trim().ToLowerInvariant();

        // Find user by username or email
        var user = await context.Users
            .FirstOrDefaultAsync(u =>
                (u.Name != null && u.Name.ToLower() == normalizedIdentifier) ||
                (u.Email != null && u.Email.ToLower() == normalizedIdentifier),
                cancellationToken);

        if (user == null)
        {
            await RecordLoginAttemptAsync(request.Identifier, false, request.IpAddress, request.UserAgent, cancellationToken);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid credentials"
            };
        }

        // Check if user is locked out
        if (user.LoginValiedTime != null && user.LoginValiedTime > DateTime.Now)
        {
            var minutes = (int)(user.LoginValiedTime.Value - DateTime.Now).TotalMinutes + 1;
            await RecordLoginAttemptAsync(request.Identifier, false, request.IpAddress, request.UserAgent, cancellationToken);
            return new LoginResult
            {
                Success = false,
                IsLockedOut = true,
                LockoutMinutes = minutes,
                ErrorMessage = $"Too many failed login attempts. Please try again in {minutes} minutes."
            };
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            // Check failed login attempts in last 30 minutes
            var failCount = await context.LoginLogs
                .CountAsync(l =>
                    l.UserName == user.Name &&
                    l.IsSuccessd == false &&
                    l.Time > DateTime.Now.AddMinutes(-30),
                    cancellationToken) + 1;

            if (failCount >= 5)
            {
                user.LoginValiedTime = DateTime.Now.AddMinutes(30);
                context.Users.Update(user);
                await context.SaveChangesAsync(cancellationToken);

                await RecordLoginAttemptAsync(request.Identifier, false, request.IpAddress, request.UserAgent, cancellationToken);
                return new LoginResult
                {
                    Success = false,
                    IsLockedOut = true,
                    LockoutMinutes = 30,
                    ErrorMessage = "Too many failed login attempts. Account locked for 30 minutes."
                };
            }

            await RecordLoginAttemptAsync(request.Identifier, false, request.IpAddress, request.UserAgent, cancellationToken);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid credentials"
            };
        }

        // Get roles
        var roles = await (from ur in context.UserRoles
                           join r in context.Roles on ur.RoleId equals r.Id
                           where ur.UserId == user.Id && r.IsEnabled && !r.IsDeleted
                           select r.Name).ToListAsync(cancellationToken);

        // Generate JWT token
        var claims = new List<Claim>
        {
            new Claim(ClaimConstant.UserId, user.Id.ToString()),
            new Claim(ClaimConstant.UserName, user.Name),

        };

        foreach (var role in roles)
        {
            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var token = _jwtTokenGenerator.GenerateToken(claims);

        await RecordLoginAttemptAsync(request.Identifier, true, request.IpAddress, request.UserAgent, cancellationToken);

        return new LoginResult
        {
            Success = true,
            Token = token,
            UserId = user.Id,
            UserName = user.Name,
            RealName = user.RealName,
            Roles = roles
        };
    }

    public async Task RecordLoginAttemptAsync(string identifier, bool success, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        context.LoginLogs.Add(new LoginLog
        {
            UserName = identifier,
            IsSuccessd = success,
            Time = DateTime.Now,
            Ip = ipAddress,
            Agent = userAgent
        });

        await context.SaveChangesAsync(cancellationToken);
    }
    public async Task<int> GetUserIdAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.GetUserId();
    }

    public async Task<string> GetUserNameAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.GetUserName();
    }

    public async Task<string?> GetUserEmailAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.GetUserEmail();
    }

    public async Task<bool> IsAdminAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.IsInRole(RoleNames.Admin.ToRoleName());
    }


    public async Task<bool> IsAuthenticatedAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated ?? false;
    }

    public async Task<UserInfoDto?> GetUserInfoAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userId = state.User.GetUserId();
        using var context = await _dbFactory.CreateDbContextAsync();
        
        var user = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserInfoDto
            {
                Id = u.Id,
                UserName = u.Name,
                RealName = u.RealName,
                Email = u.Email
            })
            .FirstOrDefaultAsync();

        return user;
    }
}
