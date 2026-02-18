using MudBlazor;
using Oikos.Web.Components.Pages.Admin.Setting.Com;

namespace Oikos.Web.Components.Pages.Admin.Setting;

public partial class SettingPage
{
    private MudListItem<SettingModel>? SelectedItem;

    private SettingModel? _selectedValue;
    private SettingModel? SelectedValue
    {
        get => _selectedValue;
        set
        {
            if (_selectedValue != value)
            {
                _selectedValue = value;
                // Ensure SelectedItem matches if set programmatically
                if (SelectedItem?.Value != value)
                {
                    // This logic depends on MudList behavior, 
                    // but usually bind-SelectedValue handles it.
                }
            }
        }
    }

    private List<SettingModel> SettingGroups = new();

    private Dictionary<string, Type> SettingComDic = new();

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SettingGroups = new List<SettingModel>
        {
            new(Loc["DashboardNewsComTitle"], Icons.Material.Filled.Campaign),
        };

        SettingComDic = new Dictionary<string, Type>
        {
            [SettingGroups[0].Name] = typeof(DashboardNewsCom),
        };

        SelectedValue = SettingGroups.FirstOrDefault();
    }

    private class SettingModel
    {
        public SettingModel(string name, string icon)
        {
            Name = name;
            Icon = icon;
        }

        public string Name { get; }

        public string Icon { get; }
    }
}
