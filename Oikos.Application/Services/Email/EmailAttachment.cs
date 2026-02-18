namespace Oikos.Application.Services.Email;

public class EmailAttachment
{
    public EmailAttachment(string fileName, byte[] content, string? contentType = null)
    {
        FileName = string.IsNullOrWhiteSpace(fileName)
            ? throw new ArgumentException("File name is required", nameof(fileName))
            : fileName;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType;
    }

    public string FileName { get; }

    public byte[] Content { get; }

    public string? ContentType { get; }
}
