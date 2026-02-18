using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oikos.Application.Services.Email;
using Oikos.Application.Services.Email.Templates;

namespace Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly IOptionsSnapshot<EmailOptions> _emailOptions;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptionsSnapshot<EmailOptions> emailOptions, ILogger<SmtpEmailSender> logger)
    {
        _emailOptions = emailOptions;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? toName = null,
        IEnumerable<EmailAttachment>? attachments = null,
        EmailConfigurationType? configurationType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("Recipient email is required", nameof(toEmail));
        }

        var emailSettings = LoadSettings(GetConfigurationName(configurationType));

        if (string.IsNullOrWhiteSpace(emailSettings.SmtpHost) || string.IsNullOrWhiteSpace(emailSettings.SenderAddress))
        {
            throw new InvalidOperationException("Email settings are not configured.");
        }

        using var message = new MailMessage()
        {
            From = new MailAddress(emailSettings.SenderAddress, string.IsNullOrWhiteSpace(emailSettings.SenderName) ? emailSettings.SenderAddress : emailSettings.SenderName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8,
        };

        message.To.Add(new MailAddress(toEmail, toName ?? string.Empty));

        if (attachments?.Any() == true)
        {
            foreach (var attachment in attachments)
            {
                if (attachment is null || attachment.Content is null || attachment.Content.Length == 0)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(attachment.FileName))
                {
                    continue;
                }

                var stream = new MemoryStream(attachment.Content);
                var contentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                    ? MediaTypeNames.Application.Octet
                    : attachment.ContentType;

                message.Attachments.Add(new Attachment(stream, attachment.FileName, contentType));
            }
        }

        using var smtpClient = new SmtpClient(emailSettings.SmtpHost, emailSettings.SmtpPort)
        {
            EnableSsl = emailSettings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
        };

        if (!string.IsNullOrWhiteSpace(emailSettings.SmtpUsername))
        {
            smtpClient.Credentials = new NetworkCredential(emailSettings.SmtpUsername, emailSettings.SmtpPassword);
        }

        try
        {
            await smtpClient.SendMailAsync(message, cancellationToken);
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Failed to resolve or reach SMTP host {Host}", emailSettings.SmtpHost);
            throw new InvalidOperationException($"Unable to reach SMTP host '{emailSettings.SmtpHost}'. Please verify the host name and network access. ({ex.Message})", ex);
        }
        catch (SmtpException ex) when (ex.InnerException is SocketException socketEx)
        {
            _logger.LogError(ex, "Failed to resolve or reach SMTP host {Host}", emailSettings.SmtpHost);
            throw new InvalidOperationException($"Unable to reach SMTP host '{emailSettings.SmtpHost}'. Please verify the host name and network access. ({socketEx.Message})", socketEx);
        }
        catch (SmtpException ex)
        {
            var smtpDetail = ex.GetBaseException()?.Message ?? ex.Message;
            _logger.LogError(ex, "SMTP error when sending email to {Email}", toEmail);
            throw new InvalidOperationException($"The email server rejected the message: {smtpDetail}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string? toName, string resetLink, DateTime expiresAtUtc, EmailConfigurationType? configurationType = null, CancellationToken cancellationToken = default)
    {
        var subject = PasswordResetEmailTemplate.Subject;
        var isBonix = configurationType == EmailConfigurationType.Bonix;
        var body = PasswordResetEmailTemplate.Render(toName ?? string.Empty, resetLink, expiresAtUtc, isBonix);
        return SendEmailAsync(toEmail, subject, body, toName, attachments: null, configurationType: configurationType, cancellationToken);
    }

    private EmailSettings LoadSettings(string? configurationName = null)
    {
        var options = string.IsNullOrWhiteSpace(configurationName)
            ? _emailOptions.Value
            : _emailOptions.Get(configurationName);

        var settings = new EmailSettings
        {
            SmtpHost = NormalizeSetting(options.SmtpHost),
            SmtpUsername = NormalizeSetting(options.SmtpUsername),
            SmtpPassword = NormalizeSetting(options.SmtpPassword),
            SenderAddress = NormalizeSetting(options.SenderAddress),
            SenderName = NormalizeSetting(options.SenderName),
        };

        settings.SmtpPort = options.SmtpPort > 0 ? options.SmtpPort : 587;
        settings.EnableSsl = options.EnableSsl;

        return settings;
    }

    private static string? NormalizeSetting(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? GetConfigurationName(EmailConfigurationType? configurationType)
    {
        return configurationType switch
        {
            EmailConfigurationType.Bonix => "EmailBonix",
            EmailConfigurationType.Default => null,
            null => null,
            _ => null
        };
    }

    private class EmailSettings
    {
        public string? SmtpHost { get; set; }

        public int SmtpPort { get; set; }

        public string? SmtpUsername { get; set; }

        public string? SmtpPassword { get; set; }

        public string? SenderAddress { get; set; }

        public string? SenderName { get; set; }

        public bool EnableSsl { get; set; } = true;
    }
}
