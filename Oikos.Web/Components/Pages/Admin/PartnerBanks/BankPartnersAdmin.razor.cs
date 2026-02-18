using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.Partner.Models;
using Oikos.Application.Services.Partner;
using Oikos.Web.Components.Pages.Admin.PartnerBanks.Dialogs;
using static Oikos.Web.Components.Shared.Pages.PagePagination;

namespace Oikos.Web.Components.Pages.Admin.PartnerBanks;

public partial class BankPartnersAdmin : ComponentBase
{
    private PartnerContent _content = new();
    private bool _isLoading = true;
    private List<BankRowModel> _pagedBanks = new();
    private SearchObject _searchObject = new();
    
    [Inject]
    private IPartnerContentService _partnerContentService { get; set; } = null!;


    private List<PartnerBank> _banks => _content.Banks;

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
        UpdatePagedBanks();
        _isLoading = false;
    }

    private async Task OpenBankDialogAsync(PartnerBank? bank)
    {
        var editingModel = bank is null ? new PartnerBank() : CloneBank(bank);

        var parameters = new DialogParameters
        {
            [nameof(PartnerBankEditor.Bank)] = editingModel
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium
        };

        var dialog = await _dialogService.ShowAsync<PartnerBankEditor>(bank is null ? Loc["BankEditor_CreateTitle"].Value : bank.Name, parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is PartnerBank updatedBank)
        {
            var existingIndex = _content.Banks.FindIndex(b => b.Id == updatedBank.Id);
            if (existingIndex >= 0)
            {
                _content.Banks[existingIndex] = updatedBank;
            }
            else
            {
                _content.Banks.Add(updatedBank);
            }

            UpdatePagedBanks();
            await PersistChangesAsync(Loc["Admin_SaveBankSuccess"].Value);
        }
    }

    private async Task DeleteBankAsync(PartnerBank bank)
    {
        bool? confirm = await _dialogService.ShowMessageBox(Loc["Admin_DeleteBankConfirmTitle"].Value,
            Loc["Admin_DeleteBankConfirmMessage", bank.Name].Value,
            yesText: Loc["Admin_DeleteConfirmYes"].Value,
            cancelText: Loc["Admin_DeleteConfirmCancel"].Value,
            options: new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall });

        if (confirm == true)
        {
            _content.Banks.RemoveAll(b => b.Id == bank.Id);
            UpdatePagedBanks();
            await PersistChangesAsync(Loc["Admin_DeleteBankSuccess"].Value);
        }
    }

    protected Task OnPageChanged(int page)
    {
        _searchObject.Page = page;
        UpdatePagedBanks();
        return Task.CompletedTask;
    }

    private void UpdatePagedBanks()
    {
        var orderedBanks = _banks.OrderBy(b => b.Name).ToList();
        _searchObject.Total = orderedBanks.Count;

        var maxPage = Math.Max(1, (_searchObject.Total - 1) / _searchObject.Size + 1);
        if (_searchObject.Page > maxPage)
        {
            _searchObject.Page = maxPage;
        }

        var skip = (_searchObject.Page - 1) * _searchObject.Size;
        _pagedBanks = orderedBanks
            .Skip(skip)
            .Take(_searchObject.Size)
            .Select((bank, index) => new BankRowModel
            {
                Number = skip + index + 1,
                Bank = bank
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

    private static PartnerBank CloneBank(PartnerBank source)
    {
        return new PartnerBank
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Details = source.Details,
            ContactInfo = source.ContactInfo,
            WebsiteUrl = source.WebsiteUrl,
            LogoUrl = source.LogoUrl,
            Regions = source.Regions.ToList()
        };
    }

    private static IEnumerable<string> SplitLines(string? value)
    {
        return value?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Enumerable.Empty<string>();
    }

    private record SearchObject : PaginationModel;

    private sealed record BankRowModel
    {
        public int Number { get; init; }

        public PartnerBank Bank { get; init; } = null!;
    }
}
