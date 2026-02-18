namespace Oikos.Application.Services.Email.Templates;

public static class SubscriptionWelcomeEmailTemplate
{
    public const string Subject = "Willkommen bei Rechtfix";

    public static string Render(string recipientName, string planName, string portalUrl)
    {
        var body = new List<string>
        {
            "Schön, dass du Mitglied wirst.",
            $"Ihr Paket \"{planName}\" wurde für Sie hinterlegt.",
            "Registrieren Sie sich einfach mit derselben E-Mail-Adresse – dann wird Ihr Paket automatisch aktiviert.",
            "Wir freuen uns, Sie in unserem Portal zu begrüßen."
        };

        var footer = "Diese Nachricht informiert Sie über Ihren Kauf bei Rechtfix.";

        return StandardEmailTemplate.Render(
            Subject,
            recipientName,
            body,
            "Zum Kundenportal",
            portalUrl,
            footer);
    }

    public static string RenderExistingUser(string recipientName, string planName, string portalUrl)
    {
        var body = new List<string>
        {
            $"Ihr Paket \"{planName}\" wurde für Ihr bestehendes Konto aktiviert.",
            "Sie können sich jetzt im Portal anmelden.",
            "Wir freuen uns, Sie weiterhin bei Rechtfix zu begrüßen."
        };

        var footer = "Diese Nachricht informiert Sie über Ihren Kauf bei Rechtfix.";

        return StandardEmailTemplate.Render(
            Subject,
            recipientName,
            body,
            "Zum Login",
            portalUrl,
            footer);
    }
}
