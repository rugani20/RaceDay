using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace RaceDay.Mvc.Filters;

/// <summary>Blocks unauthenticated users and (optionally) users in the wrong role.</summary>
public class SessionAuthAttribute : ActionFilterAttribute
{
    private readonly string? _role;
    public SessionAuthAttribute(string? role = null) { _role = role; }
    public override void OnActionExecuting(ActionExecutingContext c)
    {
        var s = c.HttpContext.Session;
        if (s.GetInt32("UserId") == null) { c.Result = new RedirectToActionResult("Login", "Account", null); return; }
        if (_role != null && s.GetString("Role") != _role) c.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
