using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.TaxOffice;
using Oikos.Application.Services.TaxOffice.Models;
using Oikos.Web.Components.Pages.Admin.TaxOffices.Dialogs;
using TaxOfficeDto = Oikos.Application.Services.TaxOffice.Models.TaxOfficeDetail;

namespace Oikos.Web.Components.Pages.Admin.TaxOffices;

public partial class TaxOfficeDetailPage
{
    [Parameter]
    public int Id { get; set; }

    [Inject]
    private ITaxOfficeService TaxOfficeService { get; set; } = null!;

    private TaxOfficeDto? _taxOffice;
    private List<TaxOfficeLicenseDto> _licenses = new();
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _taxOffice = await TaxOfficeService.GetByIdAsync(Id);
        _licenses = (await TaxOfficeService.GetLicensesAsync(Id)).ToList();
        _isLoading = false;
    }

    private async Task OpenAssignLicenseAsync()
    {
        var parameters = new DialogParameters { { nameof(AssignLicenseDialog.TaxOfficeId), Id } };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await _dialogService.ShowAsync<AssignLicenseDialog>(Loc["TaxOffice_DetailLizenzVergeben"], parameters, options);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            _licenses = (await TaxOfficeService.GetLicensesAsync(Id)).ToList();
    }
}
