using Oikos.Application.Services.Partner.Models;

namespace Oikos.Application.Services.Partner;

public interface IPartnerPortalService
{
    Task<PartnerPortalDashboardDto?> GetDashboardAsync(int userId);

    Task<List<PartnerRecommendationDto>> GetRecommendationsAsync(int userId);

    Task<(decimal CommissionPaid, int PaidCount, decimal OpenCommission, int OpenCount, List<PartnerCommissionDto> Statements)> GetCommissionsAsync(int userId);

    Task<List<PartnerSubPartnerDto>> GetSubPartnersAsync(int userId);

    Task<(bool Success, string? Error)> CreateSubPartnerAsync(int parentUserId, CreateSubPartnerRequest request);

    Task<int?> GetPartnerEntityIdAsync(int userId);
}
