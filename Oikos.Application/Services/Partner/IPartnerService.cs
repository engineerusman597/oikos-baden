using Oikos.Application.Services.Partner.Models;

namespace Oikos.Application.Services.Partner;

public interface IPartnerService
{
    Task<IReadOnlyList<PartnerDetail>> GetPartnersAsync(CancellationToken cancellationToken = default);

    Task<PartnerDetail?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<PartnerDetail> CreateAsync(PartnerRequest request, CancellationToken cancellationToken = default);

    Task<PartnerDetail> UpdateAsync(int partnerId, PartnerRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int partnerId, CancellationToken cancellationToken = default);
}
