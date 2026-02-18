using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Oikos.Application.Services.CompanyCheck;
using Oikos.Application.Services.CompanyCheck.Models;
using Oikos.Infrastructure.Constants;

namespace Oikos.Infrastructure.CompanyCheck;

public class CreditSafeClient : ICreditSafeClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BonixOptions _options;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
    private CreditSafeAccessToken? _cachedToken;

    public CreditSafeClient(IHttpClientFactory httpClientFactory, IOptionsSnapshot<BonixOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<(IReadOnlyList<CreditSafeCompanySummary> Companies, CreditSafeConfiguration Configuration)> SearchCompaniesAsync(CompanySearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var configuration = await LoadConfigurationAsync(cancellationToken);

        if (!configuration.HasCredentials)
        {
            return (Array.Empty<CreditSafeCompanySummary>(), configuration);
        }

        var accessToken = await GetAccessTokenAsync(configuration, cancellationToken);
        if (accessToken == null)
        {
            return (Array.Empty<CreditSafeCompanySummary>(), configuration);
        }

        var client = _httpClientFactory.CreateClient(CreditSafeConstants.HttpClientName);
        ConfigureHttpClient(client, configuration, accessToken.Token);

        var requestUri = new StringBuilder("companies");
        var queryParams = new List<string>();

        var countryCode = string.IsNullOrWhiteSpace(criteria.Country)
            ? configuration.DefaultCountry
            : criteria.Country;
        AddQueryParam("countries", countryCode);

        AddQueryParam("name", criteria.CompanyName);
        AddQueryParam("regNo", criteria.RegistrationNumber);
        AddQueryParam("vatNo", criteria.VatNumber);
        AddQueryParam("street", criteria.Street);
        AddQueryParam("city", criteria.City);
        AddQueryParam("postCode", criteria.PostCode);
        AddQueryParam("phoneNo", criteria.PhoneNumber);
        AddQueryParam("status", criteria.Status);
        AddQueryParam("type", criteria.CompanyType);

        if (queryParams.Count > 0)
        {
            requestUri.Append("?").Append(string.Join("&", queryParams));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri.ToString());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return (Array.Empty<CreditSafeCompanySummary>(), configuration);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var results = new List<CreditSafeCompanySummary>();
        if (document.RootElement.TryGetProperty("companies", out var companiesElement) && companiesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in companiesElement.EnumerateArray())
            {
                results.Add(ParseCompanySummary(item));
            }
        }

        return (results, configuration);

        void AddQueryParam(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                queryParams.Add($"{name}={Uri.EscapeDataString(value)}");
            }
        }
    }

    public async Task<CreditSafeConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await LoadConfigurationAsync(cancellationToken);
    }

    public async Task<(CreditSafeCompanyDetails? Details, CreditSafeConfiguration Configuration)> GetCompanyDetailsAsync(string companyId, string? countryCode, CancellationToken cancellationToken = default)
    {
        var configuration = await LoadConfigurationAsync(cancellationToken);
        if (!configuration.HasCredentials)
        {
            return (null, configuration);
        }

        var accessToken = await GetAccessTokenAsync(configuration, cancellationToken);
        if (accessToken == null)
        {
            return (null, configuration);
        }

        var client = _httpClientFactory.CreateClient(CreditSafeConstants.HttpClientName);
        ConfigureHttpClient(client, configuration, accessToken.Token);

        var requestPath = new StringBuilder("companies/");
        requestPath.Append(Uri.EscapeDataString(companyId));
        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            requestPath.Append("?countries=").Append(Uri.EscapeDataString(countryCode));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, requestPath.ToString());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return (null, configuration);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);

        var details = ParseCompanyDetails(document.RootElement);
        details.RawJson = json;
        return (details, configuration);
    }

    public async Task<(byte[]? PdfData, string? Error)> DownloadReportAsPdfAsync(
                                                            string connectId,
                                                            string language = "de",
                                                            string template = "full",
                                                            string? customData = null,
                                                            CancellationToken cancellationToken = default)
    {
        var configuration = await LoadConfigurationAsync(cancellationToken);

        if (!configuration.HasCredentials)
        {
            return (null, "Creditsafe configuration is incomplete or missing.");
        }

        var accessToken = await GetAccessTokenAsync(configuration, cancellationToken);
        if (accessToken == null)
        {
            return (null, "Unable to acquire Creditsafe access token.");
        }

        var client = _httpClientFactory.CreateClient(CreditSafeConstants.HttpClientName);
        ConfigureHttpClient(client, configuration, accessToken.Token);

        var requestPath = new StringBuilder("companies/");
        requestPath.Append(Uri.EscapeDataString(connectId));

        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(language))
        {
            queryParts.Add("language=" + Uri.EscapeDataString(language));
        }
        else
        {
            queryParts.Add("language=en");
        }

        if (!string.IsNullOrWhiteSpace(template))
        {
            queryParts.Add("template=" + Uri.EscapeDataString(template));
        }

        if (!string.IsNullOrWhiteSpace(customData))
        {
            queryParts.Add("customData=" + Uri.EscapeDataString(customData));
        }

        if (queryParts.Count > 0)
        {
            requestPath.Append("?").Append(string.Join("&", queryParts));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, requestPath.ToString());

        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var reason = await response.Content.ReadAsStringAsync(cancellationToken);
            var message = $"Creditsafe returned {(int)response.StatusCode}: {reason}";
            return (null, message);
        }

        var pdfBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            return (null, "Creditsafe returned an empty PDF document.");
        }

        return (pdfBytes, null);
    }

    private static CreditSafeCompanySummary ParseCompanySummary(JsonElement element)
    {
        var registrationNumber = element.TryGetProperty("registrationNumber", out var regElement) ? regElement.GetString() : null;
        var safeNumber = TryGetString(element, "safeNumber");
        if (string.IsNullOrWhiteSpace(registrationNumber))
        {
            registrationNumber = safeNumber;
        }

        var foundedOn = TryGetString(element, "dateOfLatestAccounts")
            ?? TryGetString(element, "incorporationDate")
            ?? TryGetNestedString(element, "companyRegistrationInformation", "incorporationDate");

        var vatNumber = TryGetString(element, "vatNumber")
            ?? TryGetNestedString(element, "companyRegistrationInformation", "vatNumber");

        var industry = TryGetString(element, "industry")
            ?? TryExtractIndustry(element);

        return new CreditSafeCompanyDetails
        {
            CompanyId = element.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty,
            Name = element.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null,
            RegistrationNumber = registrationNumber,
            SafeNumber = safeNumber,
            RegNo = TryGetString(element, "regNo"),
            SafeNo = TryGetString(element, "safeNo"),
            TradingNames = TryExtractStringArray(element, "tradingNames"),
            RegisteredCity = TryGetNestedString(element, "additionalInformation", "registeredCity"),
            Type = TryGetString(element, "type"),
            DateOfLatestChange = TryGetString(element, "dateOfLatestChange"),
            MatchScore = TryGetString(element, "matchScore"),
            StatusDescription = TryGetString(element, "statusDescription"),
            PostCode = TryGetString(element, "postCode"),
            RawJson = element.GetRawText(),
            CountryCode = TryGetString(element, "country") ?? TryGetString(element, "countryCode"),
            Address = TryExtractAddress(element),
            Status = TryGetString(element, "status") ?? TryGetString(element, "companyStatus"),
            LatestAccountsDate = TryGetString(element, "dateOfLatestAccounts")
                ?? TryGetNestedString(element, "latestAnnualAccounts", "filedOn")
                ?? TryGetNestedString(element, "latestAnnualReturn", "madeUpTo"),
            Score = TryExtractScore(element),
            FoundedOn = foundedOn,
            VatNumber = vatNumber,
            Industry = industry
        };
    }

    private static CreditSafeCompanyDetails ParseCompanyDetails(JsonElement element)
    {
        var registrationNumber = element.TryGetProperty("registrationNumber", out var regElement) ? regElement.GetString() : null;
        var safeNumber = TryGetString(element, "safeNumber");
        if (string.IsNullOrWhiteSpace(registrationNumber))
        {
            registrationNumber = safeNumber;
        }

        var score = TryExtractScore(element);
        var foundedOn = TryGetString(element, "dateOfLatestAccounts")
            ?? TryGetString(element, "incorporationDate")
            ?? TryGetNestedString(element, "companyRegistrationInformation", "incorporationDate");

        var vatNumber = TryGetString(element, "vatNumber")
            ?? TryGetNestedString(element, "companyRegistrationInformation", "vatNumber");

        var industry = TryGetString(element, "industry")
            ?? TryExtractIndustry(element);

        return new CreditSafeCompanyDetails
        {
            CompanyId = element.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty,
            Name = element.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null,
            RegistrationNumber = registrationNumber,
            CountryCode = TryGetString(element, "country") ?? TryGetString(element, "countryCode"),
            Address = TryExtractAddress(element),
            Status = TryGetString(element, "status") ?? TryGetString(element, "companyStatus"),
            SafeNumber = safeNumber,
            RegNo = TryGetString(element, "regNo"),
            SafeNo = TryGetString(element, "safeNo"),
            TradingNames = TryExtractStringArray(element, "tradingNames"),
            RegisteredCity = TryGetNestedString(element, "additionalInformation", "registeredCity"),
            Type = TryGetString(element, "type"),
            DateOfLatestChange = TryGetString(element, "dateOfLatestChange"),
            MatchScore = TryGetString(element, "matchScore"),
            StatusDescription = TryGetString(element, "statusDescription"),
            PostCode = TryGetString(element, "postCode"),
            LatestAccountsDate = TryGetString(element, "dateOfLatestAccounts")
                ?? TryGetNestedString(element, "latestAnnualAccounts", "filedOn")
                ?? TryGetNestedString(element, "latestAnnualReturn", "madeUpTo"),
            Score = score,
            FoundedOn = foundedOn,
            VatNumber = vatNumber,
            Industry = industry
        };
    }

    private static string? TryExtractAddress(JsonElement element)
    {
        if (element.TryGetProperty("address", out var addressElement))
        {
            if (addressElement.TryGetProperty("simpleValue", out var simpleValue) && simpleValue.ValueKind == JsonValueKind.String)
            {
                return simpleValue.GetString();
            }

            var parts = new List<string>();
            foreach (var propertyName in new[] { "street", "city", "postalCode", "state" })
            {
                if (addressElement.TryGetProperty(propertyName, out var part) && part.ValueKind == JsonValueKind.String)
                {
                    var value = part.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        parts.Add(value);
                    }
                }
            }

            if (parts.Count > 0)
            {
                return string.Join(", ", parts);
            }
        }

        return null;
    }

    private static List<string>? TryExtractStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        var results = new List<string>();

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var text = item.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        results.Add(text);
                    }
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                results.Add(text);
            }
        }

        return results.Count > 0 ? results : null;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    private static string? TryGetNestedString(JsonElement element, params string[] propertyPath)
    {
        var current = element;
        foreach (var segment in propertyPath)
        {
            if (!current.TryGetProperty(segment, out var next))
            {
                return null;
            }

            current = next;
        }

        if (current.ValueKind == JsonValueKind.String)
        {
            return current.GetString();
        }

        if (current.ValueKind == JsonValueKind.Number)
        {
            return current.GetRawText();
        }

        return null;
    }

    private static string? TryExtractScore(JsonElement element)
    {
        if (element.TryGetProperty("creditScore", out var creditScoreElement))
        {
            if (creditScoreElement.TryGetProperty("current", out var currentElement))
            {
                var rating = TryGetNestedString(currentElement, "creditRating")
                    ?? TryGetNestedString(currentElement, "creditRating", "value");
                if (!string.IsNullOrWhiteSpace(rating))
                {
                    return rating;
                }

                var scoreValue = TryGetNestedString(currentElement, "score");
                if (!string.IsNullOrWhiteSpace(scoreValue))
                {
                    return scoreValue;
                }
            }

            var overallScore = TryGetNestedString(creditScoreElement, "score");
            if (!string.IsNullOrWhiteSpace(overallScore))
            {
                return overallScore;
            }
        }

        return TryGetString(element, "score");
    }

    private static string? TryExtractIndustry(JsonElement element)
    {
        if (element.TryGetProperty("industryCodes", out var codesElement) && codesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var code in codesElement.EnumerateArray())
            {
                var description = TryGetString(code, "description");
                if (!string.IsNullOrWhiteSpace(description))
                {
                    return description;
                }
            }
        }

        var primaryActivity = TryGetNestedString(element, "businessActivity", "primaryNAICSDescription")
            ?? TryGetNestedString(element, "businessActivity", "primaryNaicsDescription")
            ?? TryGetNestedString(element, "businessActivity", "primarySICDescription")
            ?? TryGetNestedString(element, "businessActivity", "primarySicDescription");
        if (!string.IsNullOrWhiteSpace(primaryActivity))
        {
            return primaryActivity;
        }

        return null;
    }

    private static bool TryResolveExpiry(JsonElement root, out DateTimeOffset expiresAt)
    {
        if (TryGetDurationSeconds(root, "expiresIn", out var expiresInSeconds))
        {
            expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);
            return true;
        }

        if (TryGetExpiryTimestamp(root, "expiry", out var expiryTimestamp))
        {
            expiresAt = expiryTimestamp;
            return true;
        }

        expiresAt = default;
        return false;
    }

    private static bool TryGetDurationSeconds(JsonElement root, string propertyName, out long seconds)
    {
        if (root.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out seconds))
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.String
                && long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds))
            {
                return true;
            }
        }

        seconds = default;
        return false;
    }

    private static bool TryGetExpiryTimestamp(JsonElement root, string propertyName, out DateTimeOffset expiry)
    {
        if (root.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var seconds))
            {
                expiry = DateTimeOffset.FromUnixTimeSeconds(seconds);
                return true;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                var rawValue = property.GetString();
                if (!string.IsNullOrWhiteSpace(rawValue))
                {
                    if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var secondsFromString))
                    {
                        expiry = DateTimeOffset.FromUnixTimeSeconds(secondsFromString);
                        return true;
                    }

                    if (DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDate))
                    {
                        expiry = parsedDate;
                        return true;
                    }
                }
            }
        }

        expiry = default;
        return false;
    }

    private static void ConfigureHttpClient(HttpClient client, CreditSafeConfiguration configuration, string? accessToken = null)
    {
        if (client.BaseAddress == null && !string.IsNullOrWhiteSpace(configuration.BaseUrl))
        {
            client.BaseAddress = new Uri(configuration.BaseUrl, UriKind.Absolute);
        }

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }

    private async Task<CreditSafeAccessToken?> GetAccessTokenAsync(CreditSafeConfiguration configuration, CancellationToken cancellationToken)
    {
        if (!configuration.HasCredentials)
        {
            return null;
        }

        var cachedToken = _cachedToken;
        if (cachedToken != null && !cachedToken.ShouldRefresh)
        {
            return cachedToken;
        }

        await _tokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken != null && !_cachedToken.ShouldRefresh)
            {
                return _cachedToken;
            }

            var client = _httpClientFactory.CreateClient(CreditSafeConstants.HttpClientName);
            ConfigureHttpClient(client, configuration);

            using var request = new HttpRequestMessage(HttpMethod.Post, "authenticate");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new Dictionary<string, string?>
            {
                ["username"] = configuration.UserName,
                ["password"] = configuration.Password
            };

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _cachedToken = null;
                return null;
            }

            try
            {
                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

                if (!document.RootElement.TryGetProperty("token", out var tokenElement))
                {
                    _cachedToken = null;
                    return null;
                }

                var token = tokenElement.GetString();
                if (string.IsNullOrWhiteSpace(token))
                {
                    _cachedToken = null;
                    return null;
                }

                var expiresAt = DateTimeOffset.UtcNow.AddMinutes(50);
                if (TryResolveExpiry(document.RootElement, out var resolvedExpiry))
                {
                    expiresAt = resolvedExpiry;
                }

                _cachedToken = new CreditSafeAccessToken(token, expiresAt);
                return _cachedToken;
            }
            catch (JsonException)
            {
                _cachedToken = null;
                return null;
            }
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    private Task<CreditSafeConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        var baseUrl = _options.CreditSafeBaseUrl;
        var userName = _options.CreditSafeUsername;
        var password = _options.CreditSafePassword;
        var country = _options.CreditSafeDefaultCountry;

        var configuration = new CreditSafeConfiguration
        {
            BaseUrl = baseUrl,
            UserName = userName,
            Password = password,
            DefaultCountry = country
        };

        return Task.FromResult(configuration);
    }
}

internal sealed class CreditSafeAccessToken
{
    public CreditSafeAccessToken(string token, DateTimeOffset expiresAt)
    {
        Token = token;
        ExpiresAt = expiresAt;
    }

    public string Token { get; }

    public DateTimeOffset ExpiresAt { get; }

    public bool ShouldRefresh => DateTimeOffset.UtcNow >= ExpiresAt.Subtract(TimeSpan.FromMinutes(5));
}
