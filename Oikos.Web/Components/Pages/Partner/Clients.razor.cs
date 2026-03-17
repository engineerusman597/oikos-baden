using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Common;
using Oikos.Application.Services.Partner;
using Oikos.Application.Services.Role;
using Oikos.Application.Services.User;
using Oikos.Application.Services.User.Models;
using Oikos.Common.Constants;
using Oikos.Web.Components.Pages.Admin.User.Dialogs;

namespace Oikos.Web.Components.Pages.Partner;

public partial class Clients
{
    [Inject] private IPartnerPortalService PartnerPortalService { get; set; } = null!;
    [Inject] private IUserManagementService UserManagementService { get; set; } = null!;
    [Inject] private IRoleManagementService RoleManagementService { get; set; } = null!;

    private List<UserDto> _clients = new();
    private bool _isLoading = true;
    private int? _partnerEntityId;
    private List<int> _clientRoleIds = new();

    protected override async Task OnInitializedAsync()
    {
        var userId = await _authenticationService.GetUserIdAsync();
        _partnerEntityId = await PartnerPortalService.GetPartnerEntityIdAsync(userId);

        var allRoles = await RoleManagementService.GetRolesAsync(new() { PageSize = 100 });
        _clientRoleIds = allRoles.Items
            .Where(r => r.Name == RoleNames.User.ToRoleName() || r.Name == RoleNames.User_Bonix.ToRoleName())
            .Select(r => r.Id)
            .ToList();

        await LoadClientsAsync();
        _isLoading = false;
    }

    private async Task LoadClientsAsync()
    {
        if (!_partnerEntityId.HasValue)
        {
            _clients = new();
            return;
        }

        var criteria = new UserSearchCriteria
        {
            PartnerId = _partnerEntityId,
            RoleIds = _clientRoleIds.Any() ? _clientRoleIds : null,
            Page = 1,
            PageSize = 1000
        };

        var result = await UserManagementService.GetUsersAsync(criteria);
        _clients = result.Items.ToList();
    }

    private async Task AddClientAsync()
    {
        var parameters = new DialogParameters<CreateUserDialog>
        {
            { x => x.DefaultPartnerId, _partnerEntityId },
            { x => x.IsFromClients, true }
        };

        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseOnEscapeKey = true };
        var dialog = await _dialogService.ShowAsync<CreateUserDialog>(Loc["PartnerClients_CreateNew"], parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            await LoadClientsAsync();
            StateHasChanged();
        }
    }
}
