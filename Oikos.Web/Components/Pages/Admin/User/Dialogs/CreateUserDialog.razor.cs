using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using Oikos.Application.Services.User;
using Oikos.Application.Services.User.Models;
using Oikos.Application.Services.Partner;
using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.Subscription.Models;
using Oikos.Application.Common;
using Oikos.Common.Constants;
using Oikos.Common.Helpers;
using Oikos.Common.Resources;

namespace Oikos.Web.Components.Pages.Admin.User.Dialogs;

public partial class CreateUserDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IUserManagementService UserManagementService { get; set; } = null!;
    [Inject] private IPartnerService PartnerService { get; set; } = null!;
    [Inject] private IUserRoleService UserRoleService { get; set; } = null!;
    [Inject] private IUserPermissionService UserPermissionService { get; set; } = null!;
    [Inject] private ISubscriptionPlanService SubscriptionPlanService { get; set; } = null!;
    [Inject] private ISnackbar SnackbarService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Parameter] public bool IsFromEmployees { get; set; }
    [Parameter] public bool IsFromUsers { get; set; }
    [Parameter] public bool IsFromClients { get; set; }
    [Parameter] public int? DefaultPartnerId { get; set; }

    private CreateUserModel _model = new();
    private List<Application.Services.Partner.Models.PartnerDetail> _partners = new();
    private List<Application.Services.Role.Models.RoleDto> _roles = new();
    private List<EmployeeItem> _employees = new();
    private IReadOnlyList<SubscriptionPlanSummary> _plans = [];
    private bool _processing;

    private int? _employeeRoleId;
    private int? _adminRoleId;
    private int? _partnerRoleId;
    private bool _isEmployeeRole => _model.RoleId.HasValue && _model.RoleId == _employeeRoleId;
    private bool _isAdminRole => _model.RoleId.HasValue && _model.RoleId == _adminRoleId;
    private bool _isPartnerRole => _model.RoleId.HasValue && _model.RoleId == _partnerRoleId;
    private readonly HashSet<string> _selectedPermissions = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _partners = (await PartnerService.GetPartnersAsync()).ToList();
        _roles = await UserRoleService.GetAvailableRolesAsync();
        _plans = await SubscriptionPlanService.GetPlansAsync();

        if (IsFromUsers)
        {
            _roles = _roles.Where(r =>
                r.Name == RoleNames.Admin.ToRoleName() ||
                r.Name == RoleNames.Employee.ToRoleName()).ToList();
        }

        if (IsFromClients)
        {
            _roles = _roles.Where(r =>
                r.Name == RoleNames.User.ToRoleName() ||
                r.Name == RoleNames.User_Bonix.ToRoleName()).ToList();
        }

        var employeeRole = _roles.FirstOrDefault(r => r.Name == RoleNames.Employee.ToRoleName());
        var adminRole = _roles.FirstOrDefault(r => r.Name == RoleNames.Admin.ToRoleName());
        var partnerRole = _roles.FirstOrDefault(r => r.Name == RoleNames.Partner.ToRoleName());
        _employeeRoleId = employeeRole?.Id;
        _adminRoleId = adminRole?.Id;
        _partnerRoleId = partnerRole?.Id;

        if (_employeeRoleId.HasValue)
        {
            var employeeCriteria = new UserSearchCriteria { RoleId = _employeeRoleId, PageSize = 1000, Page = 1 };
            var employeeResult = await UserManagementService.GetUsersAsync(employeeCriteria);
            _employees = [.. employeeResult.Items
                .Select(u => new EmployeeItem { Id = u.Id, Name = u.RealName ?? u.Name })
                .OrderBy(e => e.Name)];
        }

        // Set default role to "User"
        var defaultRole = _roles.FirstOrDefault(r => r.Name == RoleNames.User.ToRoleName());
       

        if (IsFromEmployees)
        {
            _model.RoleId = _employeeRoleId;
        }
        else if (IsFromClients)
        {
            _model.RoleId = defaultRole?.Id;
        }
        else
        {
            if (defaultRole != null)
            {
                _model.RoleId = defaultRole.Id;
            }
        }

        if (DefaultPartnerId.HasValue)
        {
            _model.PartnerId = DefaultPartnerId;
        }
    }

    private void OnRoleChanged(int? roleId)
    {
        _model.RoleId = roleId;
        if (_isEmployeeRole)
        {
            _model.AssignedEmployeeId = null;
        }
        else
        {
            _selectedPermissions.Clear();
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

        var request = new CreateUserRequest
        {
            Email = _model.Email!,
            Password = string.IsNullOrWhiteSpace(_model.Password) ? null : _model.Password,
            RealName = _model.RealName?.Trim(),
            AcademicTitle = _model.AcademicTitle,
            Gender = _model.Gender,
            PhoneNumber = _model.PhoneNumber,
            Company = _model.Company,
            CustomerNumber = _model.CustomerNumber,
            PartnerId = _model.PartnerId,
            IsEnabled = _model.IsEnabled,
            Name = _model.Email!,
            RoleId = _model.RoleId,
            AssignedEmployeeId = _isEmployeeRole ? null : _model.AssignedEmployeeId,
            ApplicationBaseUrl = NavigationManager.BaseUri.TrimEnd('/')
        };

        var success = await UserManagementService.CreateUserAsync(request);

        if (success)
        {
            var criteria = new UserSearchCriteria { SearchText = _model.Email, Page = 1, PageSize = 1 };
            var users = await UserManagementService.GetUsersAsync(criteria);
            var newUser = users.Items.FirstOrDefault();

            if (newUser != null)
            {
                if (_isEmployeeRole && _selectedPermissions.Count > 0)
                    await UserPermissionService.SetUserPermissionsAsync(newUser.Id, _selectedPermissions.ToList());

                if (_model.PlanId.HasValue)
                    await SubscriptionPlanService.ActivatePlanAsync(newUser.Id, _model.PlanId.Value, _model.BillingInterval);
            }
        }

        _processing = false;

        if (success)
        {
            SnackbarService.Add(Loc["UserPage_CreateSuccess"], Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            SnackbarService.Add(Loc["UserPage_EmailDuplicate"], Severity.Error);
        }
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private string GetRoleDisplayName(string roleName)
    {
        var localized = Loc[$"Role_{roleName}"];
        return localized.ResourceNotFound ? roleName : localized.Value;
    }

    public class CreateUserModel : System.ComponentModel.DataAnnotations.IValidatableObject
    {
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
        [EmailAddress(ErrorMessageResourceName = "Validation_Email", ErrorMessageResourceType = typeof(SharedResource))]
        public string? Email { get; set; }

        public string? Password { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(Password) && Password.Length < 4)
                yield return new ValidationResult("Password must be at least 4 characters.", new[] { nameof(Password) });
        }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
        public string? RealName { get; set; }

        public string? AcademicTitle { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
        public string? Gender { get; set; }

        public string? PhoneNumber { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
        public string? Company { get; set; }

        public string? CustomerNumber { get; set; }
        public int? PartnerId { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int? RoleId { get; set; }
        public int? AssignedEmployeeId { get; set; }
        public int? PlanId { get; set; }
        public string BillingInterval { get; set; } = "monthly";
    }

    private class EmployeeItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
