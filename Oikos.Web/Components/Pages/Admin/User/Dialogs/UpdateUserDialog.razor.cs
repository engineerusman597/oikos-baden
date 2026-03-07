using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.User;
using Oikos.Application.Services.User.Models;
using Oikos.Application.Common;
using Oikos.Application.Services.Partner;
using Oikos.Application.Common;
using Oikos.Common.Constants;
using Oikos.Common.Helpers;

namespace Oikos.Web.Components.Pages.Admin.User.Dialogs;

public partial class UpdateUserDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public int UserId { get; set; }

    [Inject] private IUserManagementService UserManagementService { get; set; } = null!;
    [Inject] private IPartnerService PartnerService { get; set; } = null!;
    [Inject] private IUserRoleService UserRoleService { get; set; } = null!;
    [Inject] private IUserPermissionService UserPermissionService { get; set; } = null!;
    [Inject] private ISnackbar SnackbarService { get; set; } = null!;

    private UpdateUserModel _model = new();
    private List<Application.Services.Partner.Models.PartnerDetail> _partners = new();
    private List<Application.Services.Role.Models.RoleDto> _roles = new();
    private List<EmployeeItem> _employees = new();
    private bool _processing;
    private bool _loading = true;

    private int? _employeeRoleId;
    private bool _isEmployeeRole => _model.RoleId.HasValue && _model.RoleId == _employeeRoleId;
    private readonly HashSet<string> _selectedPermissions = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        _partners = (await PartnerService.GetPartnersAsync()).ToList();
        _roles = await UserRoleService.GetAvailableRolesAsync();

        var employeeRole = _roles.FirstOrDefault(r => r.Name == RoleNames.Employee.ToRoleName());
        _employeeRoleId = employeeRole?.Id;

        if (_employeeRoleId.HasValue)
        {
            var employeeCriteria = new UserSearchCriteria { RoleId = _employeeRoleId, PageSize = 1000, Page = 1 };
            var employeeResult = await UserManagementService.GetUsersAsync(employeeCriteria);
            _employees = employeeResult.Items
                .Select(u => new EmployeeItem { Id = u.Id, Name = u.RealName ?? u.Name })
                .OrderBy(e => e.Name)
                .ToList();
        }

        var userDetail = await UserManagementService.GetUserDetailAsync(UserId);
        if (userDetail != null)
        {
            var userRoles = await UserRoleService.GetUserRolesAsync(UserId);
            var currentRole = userRoles.FirstOrDefault();

            _model = new UpdateUserModel
            {
                Email = userDetail.Email,
                RealName = userDetail.RealName,
                AcademicTitle = userDetail.Title,
                Gender = userDetail.Gender,
                PhoneNumber = userDetail.PhoneNumber,
                Company = userDetail.Company,
                CustomerNumber = userDetail.CustomerNumber,
                PartnerId = userDetail.PartnerId,
                RoleId = currentRole?.Id,
                AssignedEmployeeId = userDetail.AssignedEmployeeId
            };

            if (_isEmployeeRole)
            {
                var existing = await UserPermissionService.GetUserPermissionsAsync(UserId);
                foreach (var p in existing) _selectedPermissions.Add(p);
            }
        }
        else
        {
            SnackbarService.Add(Loc["UserPage_UserNotFound"], Severity.Error);
            MudDialog.Cancel();
        }

        _loading = false;
    }

    private async Task OnRoleChanged(int? roleId)
    {
        _model.RoleId = roleId;
        _selectedPermissions.Clear();
        if (_isEmployeeRole)
        {
            var existing = await UserPermissionService.GetUserPermissionsAsync(UserId);
            foreach (var p in existing) _selectedPermissions.Add(p);
        }
    }

    private void OnPermissionToggled(string permission, bool value)
    {
        if (value) _selectedPermissions.Add(permission);
        else _selectedPermissions.Remove(permission);
    }

    private async Task Submit()
    {
        if (_processing) return;

        _processing = true;

        var request = new UpdateUserRequest
        {
            Email = _model.Email,
            RealName = _model.RealName?.Trim(),
            AcademicTitle = _model.AcademicTitle,
            Gender = _model.Gender,
            PhoneNumber = _model.PhoneNumber,
            Company = _model.Company,
            CustomerNumber = _model.CustomerNumber,
            PartnerId = _model.PartnerId,
            RoleId = _model.RoleId,
            AssignedEmployeeId = _model.AssignedEmployeeId
        };

        var success = await UserManagementService.UpdateUserAsync(UserId, request);

        if (success)
        {
            await UserPermissionService.SetUserPermissionsAsync(UserId,
                _isEmployeeRole ? _selectedPermissions.ToList() : new List<string>());
        }

        _processing = false;

        if (success)
        {
            SnackbarService.Add(Loc["UserPage_UpdateSuccess"], Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            SnackbarService.Add(Loc["UserPage_UserNotFound"], Severity.Error);
        }
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private class UpdateUserModel
    {
        public string? Email { get; set; }
        public string? RealName { get; set; }
        public string? AcademicTitle { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Company { get; set; }
        public string? CustomerNumber { get; set; }
        public int? PartnerId { get; set; }
        public int? RoleId { get; set; }
        public int? AssignedEmployeeId { get; set; }
    }

    private class EmployeeItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
