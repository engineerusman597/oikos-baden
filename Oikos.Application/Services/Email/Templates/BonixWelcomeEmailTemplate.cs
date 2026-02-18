namespace Oikos.Application.Services.Email.Templates;

/// <summary>
/// Email template for welcoming new Bonix users after registration
/// </summary>
public static class BonixWelcomeEmailTemplate
{
    public const string Subject = "Willkommen bei Bonix";

    public static string Render(string recipientName)
    {
        var paragraphs = new[]
        {
            "vielen Dank für Ihre Registrierung bei Bonix.",
            "Ihr Konto wurde erfolgreich angelegt. Sie können sich nun mit Ihren Zugangsdaten anmelden.",
            "Falls Sie Ihr Passwort vergessen haben, können Sie dieses jederzeit über die \"Passwort vergessen\"-Funktion zurücksetzen.",
            "Bei Fragen oder Problemen stehen wir Ihnen unter support@bonix-auskunft.de zur Verfügung."
        };

        return StandardEmailTemplate.Render(
            Subject,
            string.IsNullOrWhiteSpace(recipientName) ? "Hallo" : recipientName,
            paragraphs,
            null, // No button
            null, // No button link
            "Viele Grüße, Ihr Bonix-Team");
    }
}
