using Oikos.Application.Services.Authentication;
using Oikos.Application.Services.User;
using Cropper.Blazor.Components;
using Cropper.Blazor.Extensions;
using Cropper.Blazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Oikos.Common.Resources;

namespace Oikos.Web.Components.Layout.UserAvatar.Dialogs.Com;

public partial class AvatarEditDialog
{
    [Inject] private IAvatarStorageService _avatarStorageService { get; set; } = null!;
    [Inject] private IUserProfileService _userProfileService { get; set; } = null!;


    [Parameter] public IBrowserFile? BrowserFile { get; set; }
    [CascadingParameter] IMudDialogInstance? MudDialog { get; set; }


    private string src = "";

    private Options _options = new Options
    {
        AspectRatio = 1,
        ViewMode = ViewMode.Vm1,
    };
    private CropperComponent? cropperComponent;
    private ElementReference ElementReference;


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            _options.Preview = new ElementReference[]
            {
                ElementReference
            };

            if (BrowserFile != null && cropperComponent != null)
            {
                string oldSrc = src;

                src = await cropperComponent.GetImageUsingStreamingAsync(BrowserFile, BrowserFile.Size);


                cropperComponent?.Destroy();
                cropperComponent?.RevokeObjectUrlAsync(oldSrc);

                StateHasChanged();
            }
        }
    }

    public void OnErrorLoadImageEvent(Microsoft.AspNetCore.Components.Web.ErrorEventArgs errorEventArgs)
    {
        Destroy();
        StateHasChanged();
    }

    private void Destroy()
    {
        cropperComponent?.Destroy();
        cropperComponent?.RevokeObjectUrlAsync(src);
    }

    public async Task SaveAvatar()
    {
        GetCroppedCanvasOptions getCroppedCanvasOptions = new GetCroppedCanvasOptions
        {
            MaxHeight = 4096,
            MaxWidth = 4096,
            ImageSmoothingQuality = ImageSmoothingQuality.High.ToEnumString()
        };

        var croppedCanvasDataURL = await cropperComponent!.GetCroppedCanvasDataURLAsync(getCroppedCanvasOptions);

        var userId = await _authenticationService.GetUserIdAsync();
        var currentProfile = await _userProfileService.GetProfileSummaryAsync(userId);
        var previousAvatar = currentProfile?.Avatar;

        var storedFileName = await _avatarStorageService.SaveAvatarAsync(croppedCanvasDataURL, previousAvatar);
        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            _snackbarService.Add(Loc["AvatarUpdateFailed"], Severity.Error);
            return;
        }

        if (await _userProfileService.UpdateAvatarAsync(userId, storedFileName))
        {
            _snackbarService.Add(Loc["AvatarUpdateSuccess"], Severity.Success);
        }
        else
        {
            await _avatarStorageService.DeleteAvatarAsync(storedFileName);
            _snackbarService.Add(Loc["AvatarUpdateFailed"], Severity.Error);
        }
        MudDialog?.Close(DialogResult.Ok(true));
    }
}
