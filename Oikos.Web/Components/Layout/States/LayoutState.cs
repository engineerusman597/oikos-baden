namespace Oikos.Web.Components.Layout.States;

using static Oikos.Web.Components.Layout.NavMenus.NavItemMenu;

public interface ILayoutState
{
    event Action<NavMenuItem> NavToEvent;
    void NavigateTo(NavMenuItem item);
    
    bool NavIsOpen { get; set; }
    event Action? NavIsOpenEvent;
}

public class LayoutState : ILayoutState
{
    public event Action<NavMenuItem>? NavToEvent;

    public void NavigateTo(NavMenuItem item)
    {
        NavToEvent?.Invoke(item);
    }

    private bool _navIsOpen = true;
    public bool NavIsOpen
    {
        get => _navIsOpen;
        set
        {
            if (_navIsOpen != value)
            {
                _navIsOpen = value;
                NavIsOpenEvent?.Invoke();
            }
        }
    }

    public event Action? NavIsOpenEvent;
}
