using Microsoft.JSInterop;

namespace Oikos.Web.Components.Shared;

public partial class NoAuthorizedPage
{
    private void LogoutClick()
    {
        _navManager.NavigateTo("/api/auth/logout", forceLoad: true);
    }
}
