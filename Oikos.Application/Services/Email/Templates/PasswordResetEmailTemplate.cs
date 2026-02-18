using System.Globalization;
using System.Net;

namespace Oikos.Application.Services.Email.Templates;

public static class PasswordResetEmailTemplate
{
    public const string Subject = "Passwort zurücksetzen";

    public static string Render(string recipientName, string resetLink, DateTime expiresAtUtc, bool isBonix = false)
    {
        if (isBonix)
        {
            return RenderBonixTemplate(resetLink);
        }

        var displayName = string.IsNullOrWhiteSpace(recipientName) ? "Sie" : recipientName;
        var expiryLocal = TimeZoneInfo.ConvertTimeFromUtc(expiresAtUtc, TimeZoneInfo.Local);
        var expiryDisplay = expiryLocal.ToString("f", CultureInfo.CurrentCulture);

        var paragraphs = new[]
        {
            "Wir haben eine Anfrage zum Zurücksetzen des Passworts für Ihr Konto erhalten. Verwenden Sie die Schaltfläche unten, um ein neues Passwort festzulegen.",
            $"Wenn Sie kein Passwort-Reset angefordert haben, können Sie diese E-Mail ignorieren. Dieser Link läuft ab am {WebUtility.HtmlEncode(expiryDisplay)}.",
            "Aus Sicherheitsgründen kann der Link nur einmal verwendet werden. Wenn er abläuft, können Sie auf dem Anmeldebildschirm ein neues Passwort-Reset anfordern."
        };

        return StandardEmailTemplate.Render(
            Subject,
            displayName,
            paragraphs,
            "Passwort zurücksetzen",
            resetLink,
            "Sie erhalten diese E-Mail, weil Sie ein Passwort-Reset für Ihr Oikos-Konto angefordert haben.");
    }

    private static string RenderBonixTemplate(string resetLink)
    {
        var paragraphs = new[]
        {
            "Sie haben angefordert, Ihr Passwort für Ihr Bonix-Konto zurückzusetzen.",
            "Bitte klicken Sie auf den folgenden Link, um ein neues Passwort zu vergeben:",
            "Falls Sie diese Anfrage nicht gestellt haben, können Sie diese E-Mail ignorieren."
        };

        return StandardEmailTemplate.Render(
            Subject,
            "Hallo",
            paragraphs,
            "Hier Passwort zurücksetzen",
            resetLink,
            "Viele Grüße, Ihr Bonix-Team");
    }
}
