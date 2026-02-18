namespace Oikos.Application.Services.CompanyCheck.Models;

public class CreditSafeConfiguration
{
    public string? BaseUrl { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? DefaultCountry { get; set; }

    public bool HasCredentials => !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(UserName)
        && !string.IsNullOrWhiteSpace(Password);
}
