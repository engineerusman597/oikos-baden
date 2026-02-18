using Oikos.Application.Services.Partner.Models;
using Oikos.Application.Services.Partner;
using Microsoft.AspNetCore.Components;

namespace Oikos.Web.Components.Pages.User.BankPartners
{
    public partial class BankPartners
    {
        private List<PartnerBank> _banks = new();
        private string? _selectedRegion;
        private bool _isLoading = true;

        [Inject]
        private IPartnerContentService _partnerContentService { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var content = await _partnerContentService.GetContentAsync();
            _banks = content.Banks;
            _isLoading = false;
        }

        private IEnumerable<PartnerBank> FilteredBanks
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_selectedRegion))
                {
                    return Enumerable.Empty<PartnerBank>();
                }

                return _banks.Where(b => b.Regions.Any(r => string.Equals(r, _selectedRegion, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(b => b.Name);
            }
        }

        private static IEnumerable<string> SplitLines(string? value)
        {
            return value?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Enumerable.Empty<string>();
        }

        private static string GetRegionLabel(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return string.Empty;
            }

            return GermanRegionCatalog.Regions.FirstOrDefault(r => r.Code == code)?.Name ?? code;
        }
    }
}
