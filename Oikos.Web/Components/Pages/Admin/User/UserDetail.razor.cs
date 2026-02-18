using Microsoft.AspNetCore.Components;
using Oikos.Application.Services.User;

namespace Oikos.Web.Components.Pages.Admin.User;

public partial class UserDetail
{
    [Parameter] public int UserId { get; set; }
    [Inject] private IUserManagementService UserManagementService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private Application.Services.User.Models.UserDetailDto? _user;
    private bool _isLoading = true;

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;

        _user = await UserManagementService.GetUserDetailAsync(UserId);

        _isLoading = false;
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/admin/users");
    }
}
