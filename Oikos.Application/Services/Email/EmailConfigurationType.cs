namespace Oikos.Application.Services.Email;

/// <summary>
/// Specifies which email configuration to use for sending emails.
/// </summary>
public enum EmailConfigurationType
{
    /// <summary>
    /// Default email configuration (Rechtfix) - uses Email section in appsettings.json
    /// </summary>
    Default = 0,

    /// <summary>
    /// Bonix email configuration - uses EmailBonix section in appsettings.json
    /// </summary>
    Bonix = 1
}
