using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.Partner;
using Oikos.Application.Services.Partner.Models;

namespace Oikos.Web.Components.Pages.Partner;

public partial class Commissions
{
    [Inject] private IPartnerPortalService PartnerPortalService { get; set; } = null!;

    private decimal _commissionPaid;
    private int _paidCount;
    private decimal _openCommission;
    private int _openCount;
    private List<PartnerCommissionDto> _statements = new();
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        var userId = await _authenticationService.GetUserIdAsync();
        var result = await PartnerPortalService.GetCommissionsAsync(userId);
        _commissionPaid = result.CommissionPaid;
        _paidCount = result.PaidCount;
        _openCommission = result.OpenCommission;
        _openCount = result.OpenCount;
        _statements = result.Statements;
        _isLoading = false;
    }

    private Color GetStatementColor(string status) => status switch
    {
        "Approved" => Color.Success,
        "Pending" => Color.Warning,
        _ => Color.Default
    };

    private string GetStatementLabel(string status) => status switch
    {
        "Approved" => Loc["PartnerComm_StatusApproved"],
        "Pending" => Loc["PartnerComm_StatusPending"],
        _ => status
    };
}
