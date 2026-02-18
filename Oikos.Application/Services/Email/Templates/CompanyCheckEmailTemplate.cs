namespace Oikos.Application.Services.Email.Templates;

public static class CompanyCheckEmailTemplate
{
    public const string Subject = "Ihre bonix-Auskunft ist fertiggestellt";

    public static string Render(string recipientName, string companyName, bool isLoggedIn = false)
    {
        return BonixEmailTemplate.Render(
            Subject,
            recipientName,
            companyName,
            isLoggedIn);
    }
}

public static class BonixEmailTemplate
{
    public static string Render(string subject, string recipientName, string companyName, bool isLoggedIn)
    {
        var displayName = string.IsNullOrWhiteSpace(recipientName) ? "Sie" : recipientName.Trim();
        
        var registrationSection = !isLoggedIn
            ? """
                <p>Für noch mehr Überblick können Sie sich <strong>kostenlos registrieren</strong>. So haben Sie jederzeit Zugriff auf Ihre <strong>abgefragten Auskünfte</strong> und behalten alle Informationen zentral im Blick.</p>
                <br/>
              """
            : "";
        
        return """
            <!DOCTYPE html>
              <html lang="de">
                <head>
                  <meta charset="utf-8" />
                  <meta name="viewport" content="width=device-width, initial-scale=1" />
                  <title>{{subject}}</title>
                  <style>
                    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5; margin: 0; padding: 0; }
                    .wrapper { width: 100%; background-color: #f5f5f5; padding: 32px 0; }
                    .content { max-width: 540px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 16px rgba(15, 23, 42, 0.12); }
                    .header { background: linear-gradient(135deg, #1e88e5, #3949ab); padding: 24px; color: #ffffff; text-align: center; }
                    .header h1 { margin: 0; font-size: 22px; font-weight: 600; }
                    .body { padding: 32px 32px 24px; color: #1f2937; line-height: 1.6; }
                    .body p { margin: 0 0 12px; }
                    .footer { padding: 20px 32px 32px; font-size: 12px; color: #6b7280; text-align: center; }
                    @media (max-width: 600px) { .body { padding: 24px; } .footer { padding: 16px 24px 24px; } }
                  </style>
                </head>
                <body>
                  <div class="wrapper">
                    <div class="content">
                      <div class="header">
                        <h1>{{subject}}</h1>
                      </div>
                      <div class="body">
                        <p>Sehr geehrte Damen und Herren,</p>
                        <p>vielen Dank für Ihre Bestellung.</p>
                        <br/>
                        <p>Ihre <strong>Auskunft</strong> für die <strong>{{companyName}}</strong> ist nun fertiggestellt.</p>
                        <p>Sie finden den Bericht im Anhang dieser E-Mail.</p>
                        <br/>
                        {{registrationSection}}
                        <p>Bei Fragen oder falls Sie weitere Auskünfte benötigen, stehen wir Ihnen jederzeit gerne zur Verfügung.</p>
                        <br/>
                        <p>Mit freundlichen Grüßen<br/><strong>Ihr Bonix-Auskunft-Team</strong></p>
                      </div>
                      <div class="footer">

                      </div>
                    </div>
                  </div>
                </body>
            </html>
            """
            .Replace("{{subject}}", System.Net.WebUtility.HtmlEncode(subject))
            .Replace("{{companyName}}", System.Net.WebUtility.HtmlEncode(companyName))
            .Replace("{{registrationSection}}", registrationSection);
    }
}
