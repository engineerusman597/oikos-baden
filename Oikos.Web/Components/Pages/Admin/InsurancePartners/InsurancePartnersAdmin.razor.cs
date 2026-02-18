using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.Partner.Models;
using Oikos.Application.Services.Partner;
using Oikos.Web.Components.Pages.Admin.InsurancePartners.Dialogs;
using static Oikos.Web.Components.Shared.Pages.PagePagination;

namespace Oikos.Web.Components.Pages.Admin.InsurancePartners;

public partial class InsurancePartnersAdmin : ComponentBase
{
    private PartnerContent _content = new();
    private bool _isLoading = true;
    private List<InsuranceRowModel> _pagedPartners = new();
    private SearchObject _searchObject = new();

    [Inject]
    private IPartnerContentService _partnerContentService { get; set; } = null!;


    private List<InsurancePartner> _partners => _content.InsurancePartners;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadContentAsync();
    }

    private async Task LoadContentAsync()
    {
        _isLoading = true;
        StateHasChanged();
        _content = await _partnerContentService.GetContentAsync();
        UpdatePagedPartners();
        _isLoading = false;
    }

    private async Task OpenInsuranceDialogAsync(InsurancePartner? partner)
    {
        var editingModel = partner is null ? new InsurancePartner() : CloneInsurance(partner);

        var parameters = new DialogParameters
        {
            [nameof(InsurancePartnerEditor.Partner)] = editingModel
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium
        };

        var dialog = await _dialogService.ShowAsync<InsurancePartnerEditor>(partner is null ? Loc["InsuranceEditor_CreateTitle"].Value : partner.Name, parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is InsurancePartner updatedPartner)
        {
            var existingIndex = _content.InsurancePartners.FindIndex(p => p.Id == updatedPartner.Id);
            if (existingIndex >= 0)
            {
                _content.InsurancePartners[existingIndex] = updatedPartner;
            }
            else
            {
                _content.InsurancePartners.Add(updatedPartner);
            }

            UpdatePagedPartners();
            await PersistChangesAsync(Loc["Admin_SaveInsuranceSuccess"].Value);
        }
    }

    private async Task DeleteInsurancePartnerAsync(InsurancePartner partner)
    {
        bool? confirm = await _dialogService.ShowMessageBox(Loc["Admin_DeleteInsuranceConfirmTitle"].Value,
            Loc["Admin_DeleteInsuranceConfirmMessage", partner.Name].Value,
            yesText: Loc["Admin_DeleteConfirmYes"].Value,
            cancelText: Loc["Admin_DeleteConfirmCancel"].Value,
            options: new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall });

        if (confirm == true)
        {
            _content.InsurancePartners.RemoveAll(p => p.Id == partner.Id);
            UpdatePagedPartners();
            await PersistChangesAsync(Loc["Admin_DeleteInsuranceSuccess"].Value);
        }
    }

    protected Task OnPageChanged(int page)
    {
        _searchObject.Page = page;
        UpdatePagedPartners();
        return Task.CompletedTask;
    }

    private void UpdatePagedPartners()
    {
        var orderedPartners = _partners.OrderBy(p => p.Name).ToList();
        _searchObject.Total = orderedPartners.Count;

        var maxPage = Math.Max(1, (_searchObject.Total - 1) / _searchObject.Size + 1);
        if (_searchObject.Page > maxPage)
        {
            _searchObject.Page = maxPage;
        }

        var skip = (_searchObject.Page - 1) * _searchObject.Size;
        _pagedPartners = orderedPartners
            .Skip(skip)
            .Take(_searchObject.Size)
            .Select((partner, index) => new InsuranceRowModel
            {
                Number = skip + index + 1,
                Partner = partner
            })
            .ToList();
        StateHasChanged();
    }

    private async Task PersistChangesAsync(string message)
    {
        await _partnerContentService.SaveContentAsync(_content);
        _snackbarService.Add(message, Severity.Success);
        StateHasChanged();
    }

    private static InsurancePartner CloneInsurance(InsurancePartner source)
    {
        return new InsurancePartner
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            CoverageDetails = source.CoverageDetails,
            ContactInfo = source.ContactInfo,
            WebsiteUrl = source.WebsiteUrl,
            LogoUrl = source.LogoUrl
        };
    }

    private static IEnumerable<string> SplitLines(string? value)
    {
        return value?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Enumerable.Empty<string>();
    }

    private record SearchObject : PaginationModel;

    private sealed record InsuranceRowModel
    {
        public int Number { get; init; }

        public InsurancePartner Partner { get; init; } = null!;
    }
}
