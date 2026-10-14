using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class EnrolmentTests : ApiTestBase
{
    public EnrolmentTests(RaceDayFactory factory) : base(factory) { }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser, "Enrol Event");
        var category = await AddCategoryAsync(organiser, ev.EventID);
        var participant = await RegisterParticipantAsync();

        var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
        Assert.Equal(category.CategoryID, enrolment.CategoryID);
        Assert.Equal(ev.EventID, enrolment.EventID);
        Assert.Equal("Confirmed", enrolment.Status);

        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");
        Assert.Contains(mine!, e => e.EnrolmentID == enrolment.EnrolmentID);
    }

    [Fact]
    public async Task Enrol_SameCategoryTwice_Returns409()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);
        var category = await AddCategoryAsync(organiser, ev.EventID);
        var participant = await RegisterParticipantAsync();

        var first = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID });
        var second = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        var participant = await RegisterParticipantAsync();
        var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = 999999 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InvalidBody_Returns400()
    {
        var participant = await RegisterParticipantAsync();
        var response = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_FullCategory_Returns409()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);
        var category = await AddCategoryAsync(organiser, ev.EventID, maxEntrants: 1);

        var first = await (await RegisterParticipantAsync()).PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID });
        var second = await (await RegisterParticipantAsync()).PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanSeeEnrolmentsForOwnEvent()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);
        var category = await AddCategoryAsync(organiser, ev.EventID);
        var participant = await RegisterParticipantAsync();
        await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID });

        var enrolments = await organiser.GetFromJsonAsync<List<EnrolmentDto>>($"/api/events/{ev.EventID}/enrolments");

        Assert.Single(enrolments!);
        Assert.Equal(category.CategoryID, enrolments![0].CategoryID);
    }

    [Fact]
    public async Task OtherOrganiser_CannotSeeEnrolmentsForSomeoneElsesEvent_Returns403()
    {
        var owner = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(owner);

        var other = await LoginSecondOrganiserAsync();
        var response = await other.GetAsync($"/api/events/{ev.EventID}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment_Returns204_AndStatusBecomesCancelled()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);
        var category = await AddCategoryAsync(organiser, ev.EventID);
        var participant = await RegisterParticipantAsync();
        var enrolment = (await (await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID }))
            .Content.ReadFromJsonAsync<EnrolmentDto>())!;

        var cancel = await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentID}");
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");
        Assert.Equal("Cancelled", mine!.First(e => e.EnrolmentID == enrolment.EnrolmentID).Status);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment_Returns403()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);
        var category = await AddCategoryAsync(organiser, ev.EventID);

        var owner = await RegisterParticipantAsync();
        var enrolment = (await (await owner.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID }))
            .Content.ReadFromJsonAsync<EnrolmentDto>())!;

        var stranger = await RegisterParticipantAsync();
        var response = await stranger.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentID}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Results ----------
    [Fact]
    public async Task Organiser_CapturesResult_AndParticipantSeesItInTheirHistory()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);
        var category = await AddCategoryAsync(organiser, ev.EventID);
        var participant = await RegisterParticipantAsync();
        var enrolment = (await (await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID }))
            .Content.ReadFromJsonAsync<EnrolmentDto>())!;

        var capture = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId = enrolment.EnrolmentID, finishTime = "01:02:03", position = 5, status = "Finished" });
        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);
        var result = (await capture.Content.ReadFromJsonAsync<ResultDto>())!;

        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");
        var withResult = mine!.First(e => e.EnrolmentID == enrolment.EnrolmentID);
        Assert.Equal(5, withResult.Result!.Position);
        Assert.Equal(new TimeSpan(1, 2, 3), withResult.Result.FinishTime);

        var single = await participant.GetAsync($"/api/results/{result.ResultID}");
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409_AndUpdateWorks()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);
        var category = await AddCategoryAsync(organiser, ev.EventID);
        var participant = await RegisterParticipantAsync();
        var enrolment = (await (await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID }))
            .Content.ReadFromJsonAsync<EnrolmentDto>())!;
        var payload = new { enrolmentId = enrolment.EnrolmentID, finishTime = "00:50:00", position = 2, status = "Finished" };

        var first = await organiser.PostAsJsonAsync("/api/results", payload);
        var second = await organiser.PostAsJsonAsync("/api/results", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var created = (await first.Content.ReadFromJsonAsync<ResultDto>())!;
        var update = await organiser.PutAsJsonAsync($"/api/results/{created.ResultID}",
            new { finishTime = "00:49:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(1, (await update.Content.ReadFromJsonAsync<ResultDto>())!.Position);
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutTime_Returns400()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);
        var category = await AddCategoryAsync(organiser, ev.EventID);
        var participant = await RegisterParticipantAsync();
        var enrolment = (await (await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID }))
            .Content.ReadFromJsonAsync<EnrolmentDto>())!;

        var response = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId = enrolment.EnrolmentID, status = "Finished" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotCaptureResultForSomeoneElsesEvent_Returns403()
    {
        var owner = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(owner);
        var category = await AddCategoryAsync(owner, ev.EventID);
        var participant = await RegisterParticipantAsync();
        var enrolment = (await (await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID }))
            .Content.ReadFromJsonAsync<EnrolmentDto>())!;

        var other = await LoginSecondOrganiserAsync();
        var response = await other.PostAsJsonAsync("/api/results",
            new { enrolmentId = enrolment.EnrolmentID, finishTime = "01:00:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewAnotherParticipantsResult_Returns403()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);
        var category = await AddCategoryAsync(organiser, ev.EventID);
        var owner = await RegisterParticipantAsync();
        var enrolment = (await (await owner.PostAsJsonAsync("/api/enrolments", new { categoryId = category.CategoryID }))
            .Content.ReadFromJsonAsync<EnrolmentDto>())!;
        var result = (await (await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId = enrolment.EnrolmentID, finishTime = "01:00:00", position = 3, status = "Finished" }))
            .Content.ReadFromJsonAsync<ResultDto>())!;

        var stranger = await RegisterParticipantAsync();
        var response = await stranger.GetAsync($"/api/results/{result.ResultID}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
