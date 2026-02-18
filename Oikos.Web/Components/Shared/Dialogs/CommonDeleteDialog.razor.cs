using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Oikos.Web.Components.Shared.Dialogs;

public partial class CommonDeleteDialog
{
    [CascadingParameter] IMudDialogInstance? MudDialog { get; set; }

    [Parameter] public string? Title { get; set; }

    [Parameter] public string? ConfirmButtonText { get; set; }

    [Parameter] public EventCallback<CommonDialogEventArgs> ConfirmCallBack { get; set; }

    private bool _isLoading = false;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Title ??= Loc["CommonDeleteDialogTitle"];
        ConfirmButtonText ??= Loc["CommonDeleteDialogConfirmButtonText"];
    }

    private async Task ConfirmDelete()
    {
        _isLoading = true;
        await ConfirmCallBack.InvokeAsync(new CommonDialogEventArgs());
        _isLoading = false;
        MudDialog?.Close(DialogResult.Ok(true));
    }
}
