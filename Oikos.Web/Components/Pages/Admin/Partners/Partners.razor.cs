using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Web.Components.Pages.Partners.Partners.Dialogs;
using Oikos.Web.Components.Shared.Dialogs;
using Oikos.Application.Services.Partner;
using Oikos.Application.Services.Partner.Models;
using Microsoft.JSInterop;

namespace Oikos.Web.Components.Pages.Admin.Partners;

public partial class Partners
{
    [Inject]
    private IPartnerService PartnerService { get; set; } = null!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;


    private List<PartnerDetail> _partners = new();
    private bool _isBusy;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadPartnersAsync();
    }

    private async Task LoadPartnersAsync()
    {
        _isBusy = true;
        try
        {
            _partners = (await PartnerService.GetPartnersAsync()).ToList();
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task CreatePartnerAsync() => await OpenPartnerDialogAsync(null);

    private async Task EditPartnerAsync(PartnerDetail? partner)
    {
        if (partner == null)
        {
            return;
        }

        await OpenPartnerDialogAsync(partner);
    }

    private async Task OpenPartnerDialogAsync(PartnerDetail? partner)
    {
        var parameters = new DialogParameters
        {
            { nameof(PartnerDialog.Partner), partner }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        var title = partner == null ? Loc["PartnerDialog_CreateTitle"] : Loc["PartnerDialog_EditTitle"];
        var dialog = await _dialogService.ShowAsync<PartnerDialog>(title, parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadPartnersAsync();
        }
    }

    private async Task DeletePartnerAsync(PartnerDetail? partner)
    {
        if (partner == null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowDeleteDialog(
            Loc["PartnerManagement_DeleteConfirm", partner.Name],
            Loc["PartnerManagement_DeleteTitle"]);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await PartnerService.DeleteAsync(partner.Id);
            _snackbarService.Add(Loc["PartnerManagement_DeleteSuccess"], Severity.Success);
            await LoadPartnersAsync();
        }
        catch (Exception ex)
        {
            _snackbarService.Add(ex.Message, Severity.Error);
        }
    }

    private string GetRegularRegistrationLink(string partnerCode)
    {
        var baseUri = NavigationManager.BaseUri.TrimEnd('/');
        return $"{baseUri}/?partner={Uri.EscapeDataString(partnerCode)}";
    }

    private string GetBonixRegistrationLink(string partnerCode)
    {
        var baseUri = NavigationManager.BaseUri.TrimEnd('/');
        return $"{baseUri}/bonix-auskunft?partner={Uri.EscapeDataString(partnerCode)}";
    }

    private async Task CopyToClipboardAsync(string text, string linkType)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
            _snackbarService.Add(Loc["PartnerManagement_LinkCopied", linkType], Severity.Success);
        }
        catch (Exception)
        {
            _snackbarService.Add(Loc["PartnerManagement_CopyFailed"], Severity.Error);
        }
    }
}
