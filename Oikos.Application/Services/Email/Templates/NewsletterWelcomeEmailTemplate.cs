namespace Oikos.Application.Services.Email.Templates;

public static class NewsletterWelcomeEmailTemplate
{
    public const string Subject = "Willkommen zu unserem Newsletter";

    public static string Render(string recipientEmail)
    {
        var body = new List<string>
        {
            "Vielen Dank für Ihre Anmeldung zu unserem Newsletter!",
            "Sie erhalten ab sofort Updates über unsere neuesten Nachrichten und Angebote.",
            "Wir freuen uns, Sie an Bord zu haben!"
        };

        return StandardEmailTemplate.Render(
            Subject,
            recipientEmail, // Using email as name if name is not available
            body,
            footerText: "Sie erhalten diese E-Mail, weil Sie sich für unseren Newsletter angemeldet haben.");
    }
}
