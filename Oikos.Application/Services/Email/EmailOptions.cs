namespace Oikos.Application.Services.Email;

public class EmailOptions
{
    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    public string? SmtpUsername { get; set; }

    public string? SmtpPassword { get; set; }

    public string? SenderAddress { get; set; }

    public string? SenderName { get; set; }

    public bool EnableSsl { get; set; } = true;

    public int ResetTokenExpirationMinutes { get; set; } = 60;

    public string? SupportEmail { get; set; }
}
