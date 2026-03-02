namespace Oikos.Application.Services.Partner.Models;

public sealed record PartnerPortalDashboardDto(
    string PartnerName,
    string ReferralCode,
    int RecommendationsCount,
    decimal CommissionPaid,
    decimal OpenCommission,
    int SubPartnerCount);
