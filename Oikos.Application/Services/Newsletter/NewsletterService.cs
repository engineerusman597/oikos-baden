using Oikos.Application.Services.Email;
using Oikos.Application.Services.Email.Templates;
using Oikos.Application.Services.Newsletter.Models;

namespace Oikos.Application.Services.Newsletter;

public class NewsletterService : INewsletterService
{
    private readonly IEmailSender _emailSender;

    public NewsletterService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task SubscribeAsync(NewsletterSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var welcomeSubject = NewsletterWelcomeEmailTemplate.Subject;
        var welcomeBody = NewsletterWelcomeEmailTemplate.Render(request.Email);

        await _emailSender.SendEmailAsync(
            request.Email,
            welcomeSubject,
            welcomeBody,
            cancellationToken: cancellationToken);
    }
}
