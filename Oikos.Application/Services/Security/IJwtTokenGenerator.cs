using System.Security.Claims;

namespace Oikos.Application.Services.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(List<Claim> claims);
}
