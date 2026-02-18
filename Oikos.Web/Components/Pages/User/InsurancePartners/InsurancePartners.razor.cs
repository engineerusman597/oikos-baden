using Oikos.Application.Services.Partner.Models;
using Oikos.Application.Services.Partner;
using Microsoft.AspNetCore.Components;

namespace Oikos.Web.Components.Pages.User.InsurancePartners
{
    public partial class InsurancePartners
    {
        private List<InsurancePartner> _partners = new();
        private bool _isLoading = true;

        [Inject]
        private IPartnerContentService _partnerContentService { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var content = await _partnerContentService.GetContentAsync();
            _partners = content.InsurancePartners;
            _isLoading = false;
        }

        private static IEnumerable<string> SplitLines(string? value)
        {
            return value?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Enumerable.Empty<string>();
        }
    }
}
