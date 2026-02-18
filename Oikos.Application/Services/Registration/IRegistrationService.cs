using Oikos.Application.Services.Registration.Models;

namespace Oikos.Application.Services.Registration;

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterUserAsync(RegisterUserRequest request);
}
