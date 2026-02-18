using Oikos.Application.Services.Security;
using Oikos.Common.Helpers;

namespace Oikos.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return HashHelper.HashPassword(password);
    }

    public bool VerifyPassword(string hashedPassword, string password)
    {
        return HashHelper.VerifyPassword(hashedPassword, password);
    }
}
