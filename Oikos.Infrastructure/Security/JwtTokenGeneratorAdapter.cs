using Oikos.Application.Services.Security;
using System.Security.Claims;

namespace Oikos.Infrastructure.Security;

public class JwtTokenGeneratorAdapter : IJwtTokenGenerator
{
    private readonly JwtHelper _jwtHelper;

    public JwtTokenGeneratorAdapter(JwtHelper jwtHelper)
    {
        _jwtHelper = jwtHelper;
    }

    public string GenerateToken(List<Claim> claims)
    {
        return _jwtHelper.GenerateJwtToken(claims);
    }
}
