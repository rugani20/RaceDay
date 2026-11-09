using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;

[SessionAuth("Organiser")]
public class OrganiserController : AppController
{
    private readonly ApiClient _api;
    public OrganiserController(ApiClient api) { _api = api; }

    private async Task<List<EventDto>> MyEvents()
    {
        var r = await _api.GetAsync<List<EventDto>>("api/events/mine");


        if (!r.Success) Fail(r.Error);
        return r.Data ?? new();
    }

    public async Task<IActionResult> Dashboard() => View((await MyEvents()).OrderBy(e => e.EventDate).ToList());
    public async Task<IActionResult> Events() => View((await MyEvents()).OrderByDescending(e => e.EventDate).ToList());

    // ---------- Events ----------
    [HttpGet] public IActionResult CreateEvent() => View("EventForm", new EventFormVm());

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var r = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (!r.Success || r.Data == null) { Fail(r.Error); return RedirectToAction(nameof(Events)); }
        var e = r.Data;
        return View("EventForm", new EventFormVm { Id = e.Id, Name = e.Name, Description = e.Description, EventDate = e.EventDate, Location = e.Location, DistanceKm = e.DistanceKm, EventType = e.EventType, ExistingBannerUrl = e.BannerUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEvent(EventFormVm vm)
    {

        if (!ModelState.IsValid) return View("EventForm", vm);
        if (vm.Banner != null && (!vm.Banner.ContentType.StartsWith("image/") || vm.Banner.Length > 5_000_000))
        { ModelState.AddModelError("Banner", "Banner must be an image under 5 MB."); return View("EventForm", vm); }
        var fields = new Dictionary<string, string>
        {
            ["EventName"] = vm.Name, ["Description"] = vm.Description, ["EventDate"] = vm.EventDate.ToString("yyyy-MM-dd"),
            ["Location"] = vm.Location, ["DistanceKm"] = vm.DistanceKm.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["EventType"] = vm.EventType, ["OrganiserId"] = UserId.ToString()
        };
        var r = vm.Id == 0
            ? await _api.SendFormAsync<object>(HttpMethod.Post, "api/events", fields, vm.Banner, "banner")
            : await _api.SendFormAsync<object>(HttpMethod.Put, $"api/events/{vm.Id}", fields, vm.Banner, "banner");
        if (!r.Success) { ModelState.AddModelError("", r.Error); return View("EventForm", vm); }
        Ok(vm.Id == 0 ? "Event created." : "Event updated.");
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var r = await _api.DeleteAsync($"api/events/{id}");
        if (r.Success) Ok("Event deleted."); else Fail(r.Error);
        return RedirectToAction(nameof(Events));
    }

    // ---------- Categories ----------
    public async Task<IActionResult> Categories(int eventId)
    {
        var e = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        var c = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");
        ViewBag.EventName = e.Data?.Name ?? "Event"; ViewBag.EventId = eventId;
        return View(c.Data ?? new List<CategoryDto>());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryFormVm vm)
    {
        if (!ModelState.IsValid) Fail("Please provide a category name.");
        else
        {
            var r = await _api.PostAsync<object>("api/categories", new { eventId = vm.EventId, name = vm.Name, categoryType = vm.CategoryType });
            if (r.Success) Ok("Category added."); else Fail(r.Error);
        }
        return RedirectToAction(nameof(Categories), new { eventId = vm.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var r = await _api.DeleteAsync($"api/categories/{id}");
        if (r.Success) Ok("Category removed."); else Fail(r.Error);
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------- Enrolments ----------
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var e = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        var r = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        if (!r.Success) Fail(r.Error);
        ViewBag.EventName = e.Data?.Name ?? "Event"; ViewBag.EventId = eventId;
        return View(r.Data ?? new List<EnrolmentDto>());
    }

    // ---------- Results ----------
    [HttpGet]
    public async Task<IActionResult> Results(int eventId)
    {
        var e = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        var r = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        ViewBag.EventName = e.Data?.Name ?? "Event"; ViewBag.EventId = eventId;
        ViewBag.Concluded = e.Data != null && e.Data.EventDate.Date <= DateTime.Today;
        var existing = await _api.GetAsync<List<ResultDto>>($"api/events/{eventId}/results");
        var model = (r.Data ?? new()).Select(x =>
        {
            var res = existing.Data?.FirstOrDefault(y => y.EnrolmentId == x.Id);
            return new ResultEntryVm { EnrolmentId = x.Id, ParticipantName = x.ParticipantName, CategoryName = x.CategoryName, FinishTime = res?.FinishTime, Position = res?.Position };
        }).ToList();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Results(int eventId, List<ResultEntryVm> entries)
    {
        var valid = entries.Where(x => !string.IsNullOrWhiteSpace(x.FinishTime) && x.Position > 0).ToList();
        if (valid.Any(x => !TimeSpan.TryParse(x.FinishTime, out _))) { Fail("Finish times must look like 01:23:45 (hh:mm:ss)."); return RedirectToAction(nameof(Results), new { eventId }); }
        if (valid.Count == 0) { Fail("Enter at least one finish time and position."); return RedirectToAction(nameof(Results), new { eventId }); }
        var r = await _api.PostAsync<object>($"api/events/{eventId}/results", valid.Select(x => new { enrolmentId = x.EnrolmentId, finishTime = x.FinishTime, position = x.Position }));
        if (r.Success) Ok("Results published."); else Fail(r.Error);
        return RedirectToAction(nameof(Results), new { eventId });
    }
}
