using Microsoft.AspNetCore.Mvc;
using Oikos.Domain.Constants;

namespace Oikos.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    [HttpGet("logout")]
    public IActionResult Logout([FromQuery] string? returnUrl = null)
    {
        Response.Cookies.Delete(CommonConstant.UserToken);
        return string.IsNullOrEmpty(returnUrl) ? Redirect("/login") : Redirect(returnUrl);
    }
}
