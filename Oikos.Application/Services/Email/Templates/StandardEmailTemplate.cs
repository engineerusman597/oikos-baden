using System.Net;
using System.Text;

namespace Oikos.Application.Services.Email.Templates;

public static class StandardEmailTemplate
{
    public static string Render(
        string subject,
        string recipientName,
        IEnumerable<string> bodyParagraphs,
        string? ctaText = null,
        string? ctaUrl = null,
        string? footerText = null)
    {
        var displayName = string.IsNullOrWhiteSpace(recipientName) ? "Sie" : recipientName.Trim();
        var footer = string.IsNullOrWhiteSpace(footerText)
            ? ""
            : footerText.Trim();

        var bodyBuilder = new StringBuilder();
        foreach (var paragraph in bodyParagraphs.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            bodyBuilder.AppendLine($"<p>{WebUtility.HtmlEncode(paragraph)}</p>");
        }

        if (!string.IsNullOrWhiteSpace(ctaText) && !string.IsNullOrWhiteSpace(ctaUrl))
        {
            bodyBuilder.AppendLine($"<div class=\"cta\"><a href=\"{WebUtility.HtmlEncode(ctaUrl)}\" target=\"_blank\" rel=\"noopener noreferrer\">{WebUtility.HtmlEncode(ctaText)}</a></div>");
        }

        return """
            <!DOCTYPE html>
              <html lang="en">
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
                    .cta { margin: 32px 0; text-align: center; }
                    .cta a { display: inline-block; padding: 12px 28px; background-color: #1e88e5; color: #ffffff; text-decoration: none; border-radius: 999px; font-weight: 600; }
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
                        <p>Hallo {{displayName}},</p>
                        {{bodyContent}}
                      </div>
                      <div class="footer">
                        <p>{{footer}}</p>
                      </div>
                    </div>
                  </div>
                </body>
            </html>
            """
            .Replace("{{subject}}", WebUtility.HtmlEncode(subject))
            .Replace("{{displayName}}", WebUtility.HtmlEncode(displayName))
            .Replace("{{bodyContent}}", bodyBuilder.ToString())
            .Replace("{{footer}}", WebUtility.HtmlEncode(footer));
    }
}
