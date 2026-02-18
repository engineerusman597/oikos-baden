namespace Oikos.Application.Services.Email;

public interface IEmailSender
{
    Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? toName = null,
        IEnumerable<EmailAttachment>? attachments = null,
        EmailConfigurationType? configurationType = null,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetEmailAsync(string toEmail, string? toName, string resetLink, DateTime expiresAtUtc, EmailConfigurationType? configurationType = null, CancellationToken cancellationToken = default);
}
