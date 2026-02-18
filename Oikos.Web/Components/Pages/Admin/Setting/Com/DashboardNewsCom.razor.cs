using MudBlazor;
using Oikos.Application.Services.Setting.Models;

namespace Oikos.Web.Components.Pages.Admin.Setting.Com;

public partial class DashboardNewsCom
{
    private MudForm? _form;

    private DashboardNewsSettingsDto _model = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _model = await _settingService.GetDashboardNewsSettingsAsync();
    }

    private async Task SaveAsync()
    {
        if (_form is not null)
        {
            await _form.Validate();
            if (!_form.IsValid)
            {
                _snackbarService.Add(Loc["Validation_Required"], Severity.Error); // Or generic error
                return;
            }
        }

        try 
        {
            await _settingService.UpdateDashboardNewsSettingsAsync(_model);
            _snackbarService.Add(Loc["DashboardNewsSaveSuccess"], Severity.Success);
        }
        catch (Exception ex)
        {
            _snackbarService.Add(ex.Message, Severity.Error);
        }
    }
}
