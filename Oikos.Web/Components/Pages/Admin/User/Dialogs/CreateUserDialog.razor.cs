using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.User;
using Oikos.Application.Services.User.Models;
using Oikos.Application.Services.Partner;
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
    [Inject] private ISnackbar SnackbarService { get; set; } = null!;

    private CreateUserModel _model = new();
    private List<Application.Services.Partner.Models.PartnerDetail> _partners = new();
    private List<Application.Services.Role.Models.RoleDto> _roles = new();
    private bool _processing;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _partners = (await PartnerService.GetPartnersAsync()).ToList();
        _roles = await UserRoleService.GetAvailableRolesAsync();
        
        // Set default role to "User"
        var defaultRole = _roles.FirstOrDefault(r => r.Name == RoleNames.User.ToRoleName());
        if (defaultRole != null)
        {
            _model.RoleId = defaultRole.Id;
        }
    }

    private async Task Submit()
    {
        if (_processing) return;

        _processing = true;

        var request = new CreateUserRequest
        {
            Email = _model.Email!,
            Password = _model.Password!,
            RealName = _model.RealName?.Trim(),
            AcademicTitle = _model.AcademicTitle,
            Gender = _model.Gender,
            PhoneNumber = _model.PhoneNumber,
            Company = _model.Company,
            CustomerNumber = _model.CustomerNumber,
            PartnerId = _model.PartnerId,
            IsEnabled = _model.IsEnabled,
            Name = _model.Email!, // Using email as username
            RoleId = _model.RoleId
        };

        var success = await UserManagementService.CreateUserAsync(request);

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

    public class CreateUserModel
    {
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
        [EmailAddress(ErrorMessageResourceName = "Validation_Email", ErrorMessageResourceType = typeof(SharedResource))]
        public string? Email { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
        [MinLength(4, ErrorMessageResourceName = "Validation_MinLength", ErrorMessageResourceType = typeof(SharedResource))]
        public string? Password { get; set; }

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
    }
}
