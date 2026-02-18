using Oikos.Application.Services.Partner.Models;

namespace Oikos.Application.Services.Partner
{
    public interface IPartnerContentService
    {
        Task<PartnerContent> GetContentAsync(CancellationToken cancellationToken = default);
        Task SaveContentAsync(PartnerContent content, CancellationToken cancellationToken = default);
    }
}
