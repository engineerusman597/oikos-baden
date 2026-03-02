using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.Partner;
using Oikos.Application.Services.Partner.Models;

namespace Oikos.Web.Components.Pages.Partner;

public partial class Recommendations
{
    [Inject] private IPartnerPortalService PartnerPortalService { get; set; } = null!;

    private List<PartnerRecommendationDto> _recommendations = new();
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        var userId = await _authenticationService.GetUserIdAsync();
        _recommendations = await PartnerPortalService.GetRecommendationsAsync(userId);
        _isLoading = false;
    }

    private Color GetStatusColor(string status) => status switch
    {
        "active" => Color.Success,
        "cancelled" => Color.Error,
        "expired" => Color.Default,
        _ => Color.Default
    };

    private string GetStatusLabel(string status) => status switch
    {
        "active" => Loc["PartnerRec_StatusActive"],
        "cancelled" => Loc["PartnerRec_StatusCancelled"],
        "expired" => Loc["PartnerRec_StatusExpired"],
        _ => status
    };

    private string GetBillingLabel(string interval) => interval switch
    {
        "yearly" => Loc["PartnerRec_BillingYearly"],
        "monthly" => Loc["PartnerRec_BillingMonthly"],
        _ => interval
    };
}
