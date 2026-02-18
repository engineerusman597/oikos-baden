namespace Oikos.Application.Services.Partner.Models
{
    public class InsurancePartner
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? CoverageDetails { get; set; }

        public string? ContactInfo { get; set; }

        public string? WebsiteUrl { get; set; }

        public string? LogoUrl { get; set; }
    }
}
