using Oikos.Application.Services.User;
using Oikos.Domain.Constants;
using MudBlazor;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using static Oikos.Web.Components.Layout.NavMenus.NavItemMenu;
using Oikos.Web.Components.Layout.States;

namespace Oikos.Web.Components.Layout.NavTabs;

public partial class NavTab
{
    private List<TabView> _userTabs = new();

    private int _selectedTabIndex = 0;
    
    [Inject] private ILayoutState _layoutState { get; set; } = null!;
    [Inject] private IUserSettingService _userSettingService { get; set; } = null!;

    protected override async void OnInitialized()
    {
        base.OnInitialized();
        _layoutState.NavToEvent += async (i) => await NavigateTo(i);

        var userId = await _authenticationService.GetUserIdAsync();
        var userTabs = await _userSettingService.GetSettingAsync(userId, CommonConstant.UserTabs);
        if (userTabs != null)
        {
            _userTabs = JsonSerializer.Deserialize<List<TabView>>(userTabs) ?? new List<TabView>();
            var index = _userTabs.FindIndex(t => _navManager.Uri.EndsWith(t.Route));
            if (index == -1)
            {
                // Default to Home if current URI is not in tabs
                await NavigateTo(new NavMenuItem { MenuName = "Home", Route = "/", });
            }
            else
            {
                _selectedTabIndex = index;
            }
        }
        else
        {
            // Default to Home if no user tabs are saved
            await NavigateTo(new NavMenuItem { MenuName = "Home", Route = "/", });
        }
    }

    private async Task NavigateTo(NavMenuItem route)
    {
        if (_userTabs.All(t => t.Route != route.Route))
        {
            _userTabs.Add(new TabView
            {
                Id = Guid.NewGuid(),
                Label = route.MenuName!,
                Route = route.Route,
                ShowCloseIcon = true
            });
        }
        var index = _userTabs.FindIndex(t => t.Route == route.Route);
        _selectedTabIndex = index;
        await SaveUserTabsAsync();
        StateHasChanged();
    }

    private void TabClick(TabView tab)
    {
        _navManager.NavigateTo(tab.Route);
    }

    private async Task OnTabClose(MudTabPanel panel)
    {
        var tabView = _userTabs.FirstOrDefault(t => t.Id == (Guid)panel.ID);
        if (tabView is not null)
        {
            _userTabs.Remove(tabView);
            if (_userTabs.Any())
            {
                if (_navManager.Uri.EndsWith(tabView.Route))
                {
                    // active last userTab = 
                    var lastUserTab = _userTabs.Last();
                    _selectedTabIndex = _userTabs.Count - 1;
                    _navManager.NavigateTo(lastUserTab.Route);
                }
                else
                {
                    var index = _userTabs.FindIndex(t => _navManager.Uri.EndsWith(t.Route));
                    _selectedTabIndex = index;
                }
            }
        }
        await SaveUserTabsAsync();
    }

    private async Task SaveUserTabsAsync()
    {
        var userId = await _authenticationService.GetUserIdAsync();
        await _userSettingService.SaveSettingAsync(
            userId,
            CommonConstant.UserTabs,
            JsonSerializer.Serialize(_userTabs));
    }



    public class TabView
    {
        public string Label { get; set; } = null!;
        public Guid Id { get; set; }
        public bool ShowCloseIcon { get; set; } = true;

        public string Route { get; set; } = null!;
    }
}
