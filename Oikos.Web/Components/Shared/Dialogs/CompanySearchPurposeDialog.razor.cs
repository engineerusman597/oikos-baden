using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Oikos.Web.Components.Shared.Dialogs;

public partial class CompanySearchPurposeDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    
    [Parameter] public string? SelectedPurpose { get; set; }

    private string? _selectedPurpose;

    private List<(string Key, string Label)> _purposeOptions = new();

    protected override void OnInitialized()
    {
        // Initialize with provided value or null
        _selectedPurpose = SelectedPurpose;
        
        _purposeOptions = new List<(string, string)>
        {
            ("credit_decision", Loc["SearchPurposeOptionCreditDecision"].Value),
            ("intended_business", Loc["SearchPurposeOptionNewBusiness"].Value),
            ("existing_business", Loc["SearchPurposeOptionExistingBusiness"].Value),
            ("claim_transfer", Loc["SearchPurposeOptionClaimTransfer"].Value),
            ("purchase_agreement", Loc["SearchPurposeOptionPurchaseAgreement"].Value),
            ("trade_credit_insurance", Loc["SearchPurposeOptionTradeCreditInsurance"].Value),
            ("leasing_contract", Loc["SearchPurposeOptionLeasingContract"].Value),
            ("insurance_contract", Loc["SearchPurposeOptionInsuranceContract"].Value)
        };
    }

    private void ConfirmAsync() => MudDialog?.Close(DialogResult.Ok(_selectedPurpose));

    private void Cancel()
    {
        MudDialog?.Cancel();
    }
}
