using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Oikos.Application.Services.Subscription;

namespace Oikos.Web.Components.Pages;

public partial class RegisterSuccess
{
    private bool _isLoading = true;
    private bool _success = false;
    private string _errorMessage = "Die Mitgliedschaft konnte nicht aktiviert werden. Bitte kontaktieren Sie den Support.";

    [Inject] private ISubscriptionPlanService SubscriptionPlanService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var uri = new Uri(_navManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (!query.TryGetValue("userId", out var userIdValues) ||
            !query.TryGetValue("planId", out var planIdValues) ||
            !query.TryGetValue("billingInterval", out var billingValues))
        {
            _errorMessage = "Ungültige Parameter in der Rückgabe-URL.";
            _isLoading = false;
            return;
        }

        if (!int.TryParse(userIdValues.FirstOrDefault(), out var userId) ||
            !int.TryParse(planIdValues.FirstOrDefault(), out var planId))
        {
            _errorMessage = "Ungültige Parameter in der Rückgabe-URL.";
            _isLoading = false;
            return;
        }

        var billingInterval = billingValues.FirstOrDefault() ?? "monthly";

        try
        {
            await SubscriptionPlanService.ActivatePlanAsync(userId, planId, billingInterval, "Stripe");
            _success = true;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
