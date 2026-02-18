namespace Oikos.Application.Services.Partner.Models
{
    public class PartnerContent
    {
        public List<PartnerBank> Banks { get; set; } = new();

        public List<InsurancePartner> InsurancePartners { get; set; } = new();
    }
}
