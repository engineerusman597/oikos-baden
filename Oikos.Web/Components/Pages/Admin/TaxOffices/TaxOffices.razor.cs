using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.TaxOffice;
using Oikos.Application.Services.TaxOffice.Models;
using Oikos.Web.Components.Pages.Admin.TaxOffices.Dialogs;
using Oikos.Web.Components.Shared.Dialogs;

namespace Oikos.Web.Components.Pages.Admin.TaxOffices;

public partial class TaxOffices
{
    [Inject]
    private ITaxOfficeService TaxOfficeService { get; set; } = null!;

    private List<TaxOfficeDetail> _taxOffices = new();
    private bool _isLoading;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            _taxOffices = (await TaxOfficeService.GetTaxOfficesAsync()).ToList();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task CreateClickAsync() => await OpenDialogAsync(null);

    private async Task EditClickAsync(TaxOfficeDetail office) => await OpenDialogAsync(office);

    private async Task OpenDialogAsync(TaxOfficeDetail? office)
    {
        var parameters = new DialogParameters
        {
            { nameof(TaxOfficeDialog.TaxOffice), office }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        var title = office == null ? Loc["TaxOffice_DialogTitleCreate"] : Loc["TaxOffice_DialogTitleEdit"];
        var dialog = await _dialogService.ShowAsync<TaxOfficeDialog>(title, parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadAsync();
        }
    }

    private async Task DeleteClickAsync(TaxOfficeDetail office)
    {
        var confirmed = await _dialogService.ShowDeleteDialog(Loc["TaxOffice_DeleteTitle"]);
        if (!confirmed)
            return;

        try
        {
            await TaxOfficeService.DeleteAsync(office.Id);
            _snackbarService.Add(Loc["TaxOffice_DeleteSuccess"], Severity.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _snackbarService.Add(ex.Message, Severity.Error);
        }
    }
}
