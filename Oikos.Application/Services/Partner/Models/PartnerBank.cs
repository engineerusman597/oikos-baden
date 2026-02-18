namespace Oikos.Application.Services.Partner.Models
{
    public class PartnerBank
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Details { get; set; }

        public string? ContactInfo { get; set; }

        public string? WebsiteUrl { get; set; }

        public string? LogoUrl { get; set; }

        public List<string> Regions { get; set; } = new();
    }
}
