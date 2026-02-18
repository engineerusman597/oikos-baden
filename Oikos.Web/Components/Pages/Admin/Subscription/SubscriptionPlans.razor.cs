using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.Subscription.Models;
using Oikos.Web.Components.Shared.Dialogs;
using Oikos.Web.Components.Pages.Admin.Subscription.Dialogs;

namespace Oikos.Web.Components.Pages.Admin.Subscription;

public partial class SubscriptionPlans
{
    [Inject]
    private ISubscriptionPlanService SubscriptionPlanService { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;



    private IReadOnlyList<SubscriptionPlanDetail> _plans = Array.Empty<SubscriptionPlanDetail>();
    private bool _isBusy;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadPlansAsync();
    }

    private async Task LoadPlansAsync()
    {
        _isBusy = true;
        try
        {
            _plans = await SubscriptionPlanService.GetPlansForManagementAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task CreatePlanAsync() => await OpenPlanDialogAsync(null);

    private async Task EditPlanAsync(int? planId)
    {
        if (!planId.HasValue)
        {
            return;
        }

        var plan = _plans.FirstOrDefault(p => p.Id == planId.Value);
        if (plan == null)
        {
            return;
        }

        await OpenPlanDialogAsync(plan);
    }

    private async Task OpenPlanDialogAsync(SubscriptionPlanDetail? plan)
    {
        var parameters = new DialogParameters
        {
            { nameof(SubscriptionPlanDialog.Plan), plan },
            { nameof(SubscriptionPlanDialog.DefaultDisplayOrder), _plans.Count + 1 }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium
        };

        var title = plan == null ? Loc["SubscriptionPlansDialogCreateTitle"] : Loc["SubscriptionPlansDialogEditTitle"];
        var dialog = await DialogService.ShowAsync<SubscriptionPlanDialog>(title, parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadPlansAsync();
        }
    }

    private async Task DeletePlanAsync(int? planId)
    {
        if (!planId.HasValue)
        {
            return;
        }

        var confirmed = await DialogService.ShowDeleteDialog(Loc["SubscriptionPlansDeleteConfirm"], Loc["SubscriptionPlansDeleteTitle"]);
        if (!confirmed)
        {
            return;
        }

        try
        {
            await SubscriptionPlanService.DeletePlanAsync(planId.Value);
            Snackbar.Add(Loc["SubscriptionPlansDeleteSuccess"], Severity.Success);
            await LoadPlansAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
}
