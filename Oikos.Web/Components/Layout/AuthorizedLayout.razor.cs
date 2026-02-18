using Microsoft.AspNetCore.Components;

namespace Oikos.Web.Components.Layout;

public partial class AuthorizedLayout
{
    [Parameter] public RenderFragment? Child { get; set; }
}
