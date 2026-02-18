namespace Oikos.Application.Services.Certifier;

public interface ICertifierClient
{
    Task<string?> CreateCredentialAsync(
        string email,
        string subscriptionType,
        string? recipientName = null,
        CancellationToken cancellationToken = default);
}
