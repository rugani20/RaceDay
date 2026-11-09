using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;

[SessionAuth("Participant")]
public class ParticipantController : AppController
{
    private readonly ApiClient _api;
    public ParticipantController(ApiClient api) { _api = api; }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (categoryId == 0) { Fail("Please select a category."); return RedirectToAction("Details", "Home", new { id = eventId }); }
        var r = await _api.PostAsync<object>("api/enrolments", new { eventId, categoryId, userId = UserId });
        if (r.Success) { Ok("You are enrolled!"); return RedirectToAction(nameof(MyEnrolments)); }
        Fail(r.Error);
        return RedirectToAction("Details", "Home", new { id = eventId });
    }

    public async Task<IActionResult> MyEnrolments()
    {
        var r = await _api.GetAsync<List<EnrolmentDto>>($"api/enrolments/user/{UserId}");
        if (!r.Success) Fail(r.Error);
        return View((r.Data ?? new()).OrderBy(e => e.EventDate).ToList());
    }

    public async Task<IActionResult> MyResults()
    {
        var r = await _api.GetAsync<List<ResultDto>>($"api/results/user/{UserId}");
        if (!r.Success) Fail(r.Error);
        return View((r.Data ?? new()).OrderByDescending(e => e.EventDate).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var r = await _api.GetAsync<UserDto>($"api/users/{UserId}");
        if (!r.Success || r.Data == null) { Fail(r.Error); return RedirectToAction("Index", "Home"); }
        return View(new ProfileVm { FullName = r.Data.FullName, Email = r.Data.Email, Phone = r.Data.Phone, ProfilePictureUrl = r.Data.ProfilePictureUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var r = await _api.PutAsync<object>($"api/users/{UserId}", new { fullName = vm.FullName, phone = vm.Phone });
        if (r.Success) { HttpContext.Session.SetString("FullName", vm.FullName); Ok("Profile updated."); } else Fail(r.Error);
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile picture)
    {
        if (picture == null || picture.Length == 0 || !picture.ContentType.StartsWith("image/") || picture.Length > 5_000_000)
        { Fail("Please choose an image file under 5 MB."); return RedirectToAction(nameof(Profile)); }
        var r = await _api.SendFormAsync<object>(HttpMethod.Post, $"api/users/{UserId}/profile-picture", new(), picture, "file");
        if (r.Success) Ok("Profile picture updated."); else Fail(r.Error);
        return RedirectToAction(nameof(Profile));
    }
}
