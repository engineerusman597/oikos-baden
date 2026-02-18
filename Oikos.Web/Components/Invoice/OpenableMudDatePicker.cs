using MudBlazor;

namespace Oikos.Web.Components.Invoice;

public class OpenableMudDatePicker : MudDatePicker
{
    public void OpenPicker()
    {
        Open = true;
    }
}
