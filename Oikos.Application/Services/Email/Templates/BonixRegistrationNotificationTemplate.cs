namespace Oikos.Application.Services.Email.Templates;

/// <summary>
/// Email template for notifying support when a new Bonix user registers
/// </summary>
public static class BonixRegistrationNotificationTemplate
{
    public const string Subject = "Neue Bonix-Registrierung";

    public static string Render(string companyName, string userEmail, DateTime registrationTime)
    {
        var formattedTime = registrationTime.ToString("dd.MM.yyyy HH:mm");

        var paragraphs = new[]
        {
            "es liegt eine neue Registrierung im Bonix-System vor.",
            $"Firma: {companyName}",
            $"E-Mail: {userEmail}",
            $"Datum/Uhrzeit: {formattedTime}",
            "Bitte prüfen Sie die Registrierung im System."
        };

        return StandardEmailTemplate.Render(
            Subject,
            "", // Empty - template will use "Hallo Sie,"
            paragraphs,
            null, // No button
            null, // No button link
            "");
    }
}
