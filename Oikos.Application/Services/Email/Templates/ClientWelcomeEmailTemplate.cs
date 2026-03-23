namespace Oikos.Application.Services.Email.Templates;

public static class ClientWelcomeEmailTemplate
{
    public const string Subject = "Willkommen – Ihre Zugangsdaten";

    public static string Render(string recipientName, string email, string? password, string resetLink, DateTime expiresAtUtc)
    {
        var displayName = string.IsNullOrWhiteSpace(recipientName) ? "Sie" : recipientName;
        var expiryDisplay = expiresAtUtc.ToLocalTime().ToString("f");

        var paragraphs = new List<string>
        {
            "Ihr Konto wurde erfolgreich erstellt. Nachfolgend finden Sie Ihre Zugangsdaten:",
            $"Benutzername / E-Mail: {email}",
        };

        if (password != null)
        {
            paragraphs.Add($"Temporäres Passwort: {password}");
        }

        paragraphs.Add("Bitte setzen Sie Ihr Passwort zurück, bevor Sie sich zum ersten Mal anmelden. Klicken Sie dazu auf den Button unten.");
        paragraphs.Add($"Der Link ist bis zum {expiryDisplay} gültig und kann nur einmal verwendet werden.");

        return StandardEmailTemplate.Render(
            Subject,
            displayName,
            paragraphs,
            "Passwort jetzt festlegen",
            resetLink,
            "Sie erhalten diese E-Mail, weil ein Konto für Sie erstellt wurde.");
    }
}
