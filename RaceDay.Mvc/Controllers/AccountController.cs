using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;

public class AccountController : AppController
{
    private readonly ApiClient _api;
    public AccountController(ApiClient api) { _api = api; }

    [HttpGet] public IActionResult Login() => Role != "" ? RedirectToHome() : View(new LoginVm());

    [HttpPost, ValidateAntiForgeryToken]

    public async Task<IActionResult> Login(LoginVm vm)

    {

        if (!ModelState.IsValid) return View(vm);

        var r = await _api.PostAsync<LoginResponse>("api/auth/login", new { email = vm.Email, password = vm.Password });

        if (!r.Success || r.Data?.User == null) { ModelState.AddModelError("", r.Error); return View(vm); }

        var u = r.Data.User;

        HttpContext.Session.SetInt32("UserId", u.UserID);

        HttpContext.Session.SetString("FullName", u.FullName);

        HttpContext.Session.SetString("Role", u.Role);

        Ok($"Welcome back, {u.FullName}!");

        return RedirectToHome();

    }



    [HttpGet] public IActionResult Register() => View(new RegisterVm());

    [HttpPost, ValidateAntiForgeryToken]

    public async Task<IActionResult> Register(RegisterVm vm)

    {

        if (!ModelState.IsValid) return View(vm);

        var r = await _api.PostAsync<object>("api/auth/register", new { fullName = vm.FullName, email = vm.Email, password = vm.Password, role = vm.Role, phoneNumber = vm.Phone, city = vm.City, organiserCode = vm.OrganiserCode });

        if (!r.Success) { ModelState.AddModelError("", r.Error); return View(vm); }

        Ok("Account created. Please log in.");

        return RedirectToAction(nameof(Login));

    }



    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.PostAsync<object>("api/auth/logout", null); // best effort
        HttpContext.Session.Clear();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToHome() =>
        Role == "Organiser" ? RedirectToAction("Dashboard", "Organiser") : RedirectToAction("Index", "Home");
}
