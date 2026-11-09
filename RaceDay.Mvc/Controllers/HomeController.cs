using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;

public class HomeController : AppController
{
    private readonly ApiClient _api;
    public HomeController(ApiClient api) { _api = api; }

    // Participant home: all upcoming events with filtering. Public so visitors can browse.
    public async Task<IActionResult> Index(string? search, string? type, DateTime? from)
    {
        if (Role == "Organiser") return RedirectToAction("Dashboard", "Organiser");
        var r = await _api.GetAsync<List<EventDto>>("api/events");
        if (!r.Success) Fail(r.Error);
        var list = (r.Data ?? new()).Where(e => e.EventDate.Date >= DateTime.Today).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search)) list = list.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(type)) list = list.Where(e => e.EventType == type);
        if (from.HasValue) list = list.Where(e => e.EventDate.Date >= from.Value.Date);
        ViewBag.Search = search; ViewBag.Type = type; ViewBag.From = from?.ToString("yyyy-MM-dd");
        return View(list.OrderBy(e => e.EventDate).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var e = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (!e.Success || e.Data == null) { Fail(e.Error); return RedirectToAction(nameof(Index)); }
        var c = await _api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
        ViewBag.Categories = c.Data ?? new List<CategoryDto>();
        return View(e.Data);
    }

    public IActionResult Error() => View();
}
