using Oikos.Domain.Constants;
using System.Security.Claims;

namespace Oikos.Application.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var userId = user.Claims.FirstOrDefault(c => c.Type == ClaimConstant.UserId)!.Value;
        return int.Parse(userId);
    }

    public static string GetUserName(this ClaimsPrincipal user)
    {
        return user.Claims.FirstOrDefault(c => c.Type == ClaimConstant.UserName)!.Value;
    }

    public static string? GetUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value
            ?? user.FindFirst(ClaimConstant.UserName)?.Value
            ?? user.Identity?.Name;
    }
}
