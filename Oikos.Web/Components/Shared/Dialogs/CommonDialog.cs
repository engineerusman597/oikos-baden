using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Oikos.Web.Components.Shared.Dialogs
{
    public static class CommonDialog
    {
        public delegate Task DialogMethodDelegate(CommonDialogEventArgs args);

        /// <summary>
        /// Show a delete confirmation dialog.
        /// </summary>
        /// <param name="dialogService"></param>
        /// <param name="title">Dialog title.</param>
        /// <param name="confirmButtonText">Confirm button text.</param>
        /// <param name="confirmCallBackMethod">Callback executed before the dialog closes. Use the return value to decide whether to continue.</param>
        /// <returns>True when the action is confirmed.</returns>
        public static async Task<bool> ShowDeleteDialog(this IDialogService dialogService,
            string? title = null, string? confirmButtonText = null, DialogMethodDelegate? confirmCallBackMethod = null)
        {
            var confirmCallBack = new EventCallback<CommonDialogEventArgs>(null,
                async (CommonDialogEventArgs e) =>
                {
                    if (confirmCallBackMethod != null)
                    {
                        await confirmCallBackMethod(e);
                    }
                    ;
                });
            var parameters = new DialogParameters
            {
                {"Title", title},
                {"ConfirmButtonText", confirmButtonText},
                {"ConfirmCallBack", confirmCallBack }
            };
            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraLarge, NoHeader = true, };
            var dialog = await dialogService.ShowAsync<CommonDeleteDialog>(string.Empty, parameters, options);
            var result = await dialog.Result;
            return !result.Canceled;
        }


    }
}
