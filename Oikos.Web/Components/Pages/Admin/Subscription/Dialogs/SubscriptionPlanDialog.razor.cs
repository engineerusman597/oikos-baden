using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.Subscription.Models;

namespace Oikos.Web.Components.Pages.Admin.Subscription.Dialogs;

public partial class SubscriptionPlanDialog
{
    private sealed class PlanEditorModel
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public int? MonthlyClaimLimit { get; set; }
        public int? MonthlyBionicCheckLimit { get; set; }
        public int? TeamSeatLimit { get; set; }
        public int DisplayOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;

        public static PlanEditorModel FromDetail(SubscriptionPlanDetail detail)
        {
            return new PlanEditorModel
            {
                Id = detail.Id,
                Name = detail.Name,
                Slug = detail.Slug,
                Description = detail.Description,
                MonthlyPrice = detail.MonthlyPrice,
                YearlyPrice = detail.YearlyPrice,
                MonthlyClaimLimit = detail.MonthlyClaimLimit,
                MonthlyBionicCheckLimit = detail.MonthlyBionicCheckLimit,
                TeamSeatLimit = detail.TeamSeatLimit,
                DisplayOrder = detail.DisplayOrder,
                IsActive = detail.IsActive
            };
        }

        public SubscriptionPlanDetail ToDetail()
        {
            return new SubscriptionPlanDetail(
                Id,
                Name,
                Slug,
                Description,
                MonthlyPrice,
                YearlyPrice,
                MonthlyClaimLimit,
                MonthlyBionicCheckLimit,
                TeamSeatLimit,
                DisplayOrder,
                IsActive);
        }
    }

    [CascadingParameter] IMudDialogInstance? MudDialog { get; set; }

    [Parameter] public SubscriptionPlanDetail? Plan { get; set; }

    [Parameter] public int DefaultDisplayOrder { get; set; } = 1;

    [Inject] private ISubscriptionPlanService SubscriptionPlanService { get; set; } = null!;

    [Inject] private ISnackbar Snackbar { get; set; } = null!;





    private PlanEditorModel _model = new();
    private bool _isBusy;
    private MudForm? _form;

    protected override void OnInitialized()
    {
        if (Plan != null)
        {
            _model = PlanEditorModel.FromDetail(Plan);
        }
        else
        {
            _model = new PlanEditorModel
            {
                DisplayOrder = DefaultDisplayOrder,
                IsActive = true
            };
        }
    }

    private async Task SaveAsync()
    {
        if (_form is not null)
        {
            await _form.Validate();
            if (!_form.IsValid)
            {
                return;
            }
        }

        _isBusy = true;
        try
        {
            var saved = await SubscriptionPlanService.SavePlanAsync(_model.ToDetail());
            Snackbar.Add(Loc["SubscriptionPlansSaveSuccess"], Severity.Success);
            MudDialog?.Close(DialogResult.Ok(saved));
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void Cancel() => MudDialog?.Cancel();
}
