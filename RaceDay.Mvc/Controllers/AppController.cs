using Microsoft.AspNetCore.Mvc;
namespace RaceDay.Mvc.Controllers;
public abstract class AppController : Controller
{
    protected int UserId => HttpContext.Session.GetInt32("UserId") ?? 0;
    protected string Role => HttpContext.Session.GetString("Role") ?? "";
    protected void Ok(string m) => TempData["Success"] = m;
    protected void Fail(string m) => TempData["Error"] = m;
}
