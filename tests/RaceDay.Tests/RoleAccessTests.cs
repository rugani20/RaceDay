using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

/// <summary>Role-based access: unauthenticated users get 401, the wrong role gets 403.</summary>
public class RoleAccessTests : ApiTestBase
{
    public RoleAccessTests(RaceDayFactory factory) : base(factory) { }

    [Fact]
    public async Task Unauthenticated_CreateEvent_Returns401()
    {
        var response = await NewClient().PostAsJsonAsync("/api/events", NewEventPayload());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Enrol_Returns401()
    {
        var response = await NewClient().PostAsJsonAsync("/api/enrolments", new { categoryId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CreateEvent_Returns403()
    {
        var participant = await RegisterParticipantAsync();
        var response = await participant.PostAsJsonAsync("/api/events", NewEventPayload());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_UpdateOrDeleteEvent_Returns403()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);

        var participant = await RegisterParticipantAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.PutAsJsonAsync($"/api/events/{ev.EventID}", NewEventPayload())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.DeleteAsync($"/api/events/{ev.EventID}")).StatusCode);
    }

    [Fact]
    public async Task Participant_AddCategory_Returns403()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);

        var participant = await RegisterParticipantAsync();
        var response = await participant.PostAsJsonAsync($"/api/events/{ev.EventID}/categories",
            new { categoryName = "Nope", distanceKm = 5, entryFee = 10 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewEventEnrolments_Returns403()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);

        var participant = await RegisterParticipantAsync();
        var response = await participant.GetAsync($"/api/events/{ev.EventID}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CaptureResult_Returns403()
    {
        var participant = await RegisterParticipantAsync();
        var response = await participant.PostAsJsonAsync("/api/results",
            new { enrolmentId = 1, finishTime = "01:00:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_Enrol_Returns403()
    {
        var organiser = await LoginOrganiserAsync();
        var response = await organiser.PostAsJsonAsync("/api/enrolments", new { categoryId = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewParticipantEnrolments_Returns403()
    {
        var organiser = await LoginOrganiserAsync();
        var response = await organiser.GetAsync("/api/enrolments/me");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
