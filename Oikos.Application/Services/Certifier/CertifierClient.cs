using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oikos.Application.Constants;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Oikos.Application.Services.Certifier;

public class CertifierClient : ICertifierClient
{
    private readonly HttpClient _httpClient;
    private readonly CertifierOptions _options;
    private readonly ILogger<CertifierClient> _logger;

    public CertifierClient(HttpClient httpClient, IOptions<CertifierOptions> options, ILogger<CertifierClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        var baseAddress = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? new Uri($"{CertifierConstants.BaseUrl.TrimEnd('/')}/")
            : new Uri(_options.BaseUrl.TrimEnd('/') + "/");

        _httpClient.BaseAddress = baseAddress;

        if (!_httpClient.DefaultRequestHeaders.Contains(CertifierConstants.VersionHeaderName))
        {
            _httpClient.DefaultRequestHeaders.Add(
                CertifierConstants.VersionHeaderName,
                CertifierConstants.VersionHeaderValue);
        }
    }

    public async Task<string?> CreateCredentialAsync(
        string email,
        string subscriptionType,
        string? recipientName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Certifier API credentials are not configured");
            return null;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "credentials");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new CertifierRequest
        {
            Email = email,
            SubscriptionType = subscriptionType,
            Name = string.IsNullOrWhiteSpace(recipientName) ? null : recipientName.Trim()
        };

        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Certifier API returned {StatusCode}: {Content}", response.StatusCode, content);
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<CertifierResponse>(cancellationToken: cancellationToken);

        if (body == null || string.IsNullOrWhiteSpace(body.CredentialId))
        {
            _logger.LogWarning("Certifier API response did not contain a credential id");
            return null;
        }

        return body.CredentialId;
    }

    private class CertifierRequest
    {
        public string Email { get; set; } = string.Empty;

        public string SubscriptionType { get; set; } = string.Empty;

        public string? Name { get; set; }
    }

    private class CertifierResponse
    {
        public string? CredentialId { get; set; }
    }
}
