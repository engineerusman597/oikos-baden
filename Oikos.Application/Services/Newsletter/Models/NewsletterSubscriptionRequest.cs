namespace Oikos.Application.Services.Newsletter.Models;

public class NewsletterSubscriptionRequest
{
    public required string Email { get; set; }
    public string? Language { get; set; }
}
