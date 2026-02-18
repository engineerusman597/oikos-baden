using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oikos.Application.Constants;

namespace Oikos.Application.Services.Certifier;

public class CertifierVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CertifierVerificationService> _logger;
    private readonly CertifierOptions _options;

    public CertifierVerificationService(
        HttpClient httpClient,
        IOptions<CertifierOptions> options,
        ILogger<CertifierVerificationService> logger)
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

    public async Task<CertifierVerificationResult> ValidateCertificateAsync(
        string certificateCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return CertifierVerificationResult.MissingApiKey;
        }

        if (string.IsNullOrWhiteSpace(certificateCode))
        {
            return CertifierVerificationResult.Invalid;
        }

        var trimmedCertificateCode = certificateCode.Trim();

        try
        {
            var jsonBody = 
                "{\"filter\":{\"AND\":[{\"publicId\":{\"equals\":\"" +
                trimmedCertificateCode +
                "\"}}]},\"limit\":1}";

            using var request = new HttpRequestMessage(HttpMethod.Post, "credentials/search")
            {
                Headers =
                {
                    { "accept", "application/json" },
                    { "authorization", $"Bearer {_options.ApiKey}" }
                },
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                if (IsSearchResultValid(payload))
                {
                    return CertifierVerificationResult.Valid;
                }

                _logger.LogWarning(
                    "Certifier validation returned no matches for code {CertificateCode}",
                    certificateCode);

                return CertifierVerificationResult.Invalid;
            }

            _logger.LogWarning(
                "Certifier validation failed for code {CertificateCode} with status {StatusCode}: {Content}",
                certificateCode,
                response.StatusCode,
                payload);

            return CertifierVerificationResult.Invalid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating certificate code with Certifier.io");
            return CertifierVerificationResult.Unavailable;
        }
    }

    private static bool IsSearchResultValid(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                return root.GetArrayLength() > 0;
            }

            if (root.TryGetProperty("total", out var totalProperty) &&
                totalProperty.ValueKind == JsonValueKind.Number &&
                totalProperty.TryGetInt32(out var total) && total > 0)
            {
                return true;
            }

            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array &&
                    property.Value.GetArrayLength() > 0)
                {
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }
}

public record CertifierVerificationResult(bool IsValid, bool ApiKeyMissing, bool ServiceUnavailable)
{
    public static readonly CertifierVerificationResult Valid = new(true, false, false);
    public static readonly CertifierVerificationResult Invalid = new(false, false, false);
    public static readonly CertifierVerificationResult MissingApiKey = new(false, true, false);
    public static readonly CertifierVerificationResult Unavailable = new(false, false, true);
}
