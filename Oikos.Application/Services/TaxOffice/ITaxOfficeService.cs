using Oikos.Application.Services.TaxOffice.Models;

namespace Oikos.Application.Services.TaxOffice;

public interface ITaxOfficeService
{
    Task<IReadOnlyList<TaxOfficeDetail>> GetTaxOfficesAsync(CancellationToken cancellationToken = default);
    Task<TaxOfficeDetail?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TaxOfficeDetail> CreateAsync(TaxOfficeRequest request, CancellationToken cancellationToken = default);
    Task<TaxOfficeDetail> UpdateAsync(int id, TaxOfficeRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxOfficeLicenseDto>> GetLicensesAsync(int taxOfficeId, CancellationToken cancellationToken = default);
    Task AssignLicenseAsync(int taxOfficeId, string companyName, string? contactPerson, string? email, string? phoneNumber, int planId, string billingInterval, string? paymentMethod, CancellationToken cancellationToken = default);
}
