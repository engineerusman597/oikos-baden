using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Web.Components.Pages.Admin.InvoiceStages.Dialogs;
using Oikos.Application.Services.Invoice;
using Oikos.Application.Services.Invoice.Models;

namespace Oikos.Web.Components.Pages.Admin.InvoiceStages;

public partial class InvoiceStages
{
    [Inject] private IInvoiceManagementService _invoiceService { get; set; } = null!;
    // [Inject] private IAccessService _accessService { get; set; } = null!; // Not used anymore

    private readonly List<InvoiceStageListDto> _stages = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadStagesAsync();
    }

    private async Task LoadStagesAsync()
    {
        var stages = await _invoiceService.GetStageListAsync();
        _stages.Clear();
        _stages.AddRange(stages);
    }

    private async Task AddStageAsync()
    {

        var parameters = new DialogParameters
        {
            [nameof(EditInvoiceStageDialog.StageId)] = null
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = false
        };

        var dialog = await _dialogService.ShowAsync<EditInvoiceStageDialog>(Loc["StageManagerAddDialogTitle"], parameters, options);
        var result = await dialog.Result;
        if (!result.Canceled)
        {
            await LoadStagesAsync();
        }
    }

    private async Task EditStageAsync(int stageId)
    {

        var parameters = new DialogParameters
        {
            [nameof(EditInvoiceStageDialog.StageId)] = stageId
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = false
        };

        var dialog = await _dialogService.ShowAsync<EditInvoiceStageDialog>(Loc["StageManagerEditDialogTitle"], parameters, options);
        var result = await dialog.Result;
        if (!result.Canceled)
        {
            await LoadStagesAsync();
        }
    }

    private async Task DeleteStageAsync(InvoiceStageListDto stage)
    {

        var confirm = await _dialogService.ShowMessageBox(
            Loc["StageManagerDeleteDialogTitle"],
            string.Format(Loc["StageManagerDeleteDialogMessage"], stage.Name),
            yesText: Loc["CommonDelete"],
            cancelText: Loc["CommonCancel"],
            options: new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true });

        if (confirm != true)
        {
            return;
        }

        var success = await _invoiceService.DeleteStageAsync(stage.Id);
        
        if (!success)
        {
            _snackbarService.Add(Loc["StageManagerDeleteBlocked"], Severity.Error);
            return;
        }

        _snackbarService.Add(Loc["StageManagerDeleteSuccess"], Severity.Success);
        await LoadStagesAsync();
    }

    private async Task MoveStageAsync(InvoiceStageListDto stage, int offset)
    {

        var success = await _invoiceService.MoveStageAsync(stage.Id, offset);
        
        if (!success)
        {
            _snackbarService.Add(Loc["StageManagerNotFound"], Severity.Error);
        }

        await LoadStagesAsync();
    }

    private string FormatDateTime(DateTime date)
        => date.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture);
}
