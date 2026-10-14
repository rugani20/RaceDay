using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class EventTests : ApiTestBase
{
    public EventTests(RaceDayFactory factory) : base(factory) { }

    [Fact]
    public async Task GetEvents_IsPublic_AndReturnsUpcomingSeededEvents()
    {
        var events = await NewClient().GetFromJsonAsync<List<EventDto>>("/api/events");

        Assert.NotNull(events);
        Assert.Contains(events!, e => e.EventName == "Cape Town Cycle Tour");
        Assert.DoesNotContain(events!, e => e.EventName == "Soweto Marathon"); // completed, in the past
    }

    [Fact]
    public async Task GetEvents_WithIncludePast_ReturnsPastEventsToo()
    {
        var events = await NewClient().GetFromJsonAsync<List<EventDto>>("/api/events?includePast=true");
        Assert.Contains(events!, e => e.EventName == "Soweto Marathon");
    }

    [Fact]
    public async Task GetEvents_FilterByType_OnlyReturnsThatType()
    {
        var events = await NewClient().GetFromJsonAsync<List<EventDto>>("/api/events?includePast=true&eventType=Cycling");
        Assert.NotEmpty(events!);
        Assert.All(events!, e => Assert.Equal("Cycling", e.EventType));
    }

    [Fact]
    public async Task GetEvent_Existing_ReturnsDetailsWithCategoriesAndRoutes()
    {
        var all = await NewClient().GetFromJsonAsync<List<EventDto>>("/api/events");
        var cycle = all!.First(e => e.EventName == "Cape Town Cycle Tour");

        var detail = await NewClient().GetFromJsonAsync<EventDetailDto>($"/api/events/{cycle.EventID}");

        Assert.Equal(2, detail!.Categories.Count);
        Assert.Single(detail.Routes);
    }

    [Fact]
    public async Task GetEvent_Unknown_Returns404()
    {
        var response = await NewClient().GetAsync("/api/events/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateUpdateAndDeleteAnEvent()
    {
        var organiser = await LoginOrganiserAsync();

        var create = await organiser.PostAsJsonAsync("/api/events", NewEventPayload("Lifecycle Event"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await create.Content.ReadFromJsonAsync<EventDto>())!;
        Assert.Equal("Lifecycle Event", created.EventName);

        var update = await organiser.PutAsJsonAsync($"/api/events/{created.EventID}", NewEventPayload("Lifecycle Event Renamed"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<EventDto>())!;
        Assert.Equal("Lifecycle Event Renamed", updated.EventName);

        var delete = await organiser.DeleteAsync($"/api/events/{created.EventID}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var afterDelete = await NewClient().GetAsync($"/api/events/{created.EventID}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_MissingRequiredFields_Returns400()
    {
        var organiser = await LoginOrganiserAsync();
        var response = await organiser.PostAsJsonAsync("/api/events", new { eventName = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        var organiser = await LoginOrganiserAsync();
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "Swim Gala", eventType = "Swimming", eventDate = "2027-05-01", location = "Durban"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_Unknown_Returns404()
    {
        var organiser = await LoginOrganiserAsync();
        var response = await organiser.PutAsJsonAsync("/api/events/999999", NewEventPayload());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEditOrDeleteAnotherOrganisersEvent_Returns403()
    {
        var owner = await LoginOrganiserAsync();
        var created = await CreateEventAsync(owner, "Owned By Thabo");

        var other = await LoginSecondOrganiserAsync();
        var put = await other.PutAsJsonAsync($"/api/events/{created.EventID}", NewEventPayload("Hijacked"));
        var delete = await other.DeleteAsync($"/api/events/{created.EventID}");

        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanAddCategory_AndAnyoneCanListIt()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser, "Category Event");
        var category = await AddCategoryAsync(organiser, ev.EventID);

        var list = await NewClient().GetFromJsonAsync<List<CategoryDto>>($"/api/events/{ev.EventID}/categories");
        Assert.Contains(list!, c => c.CategoryID == category.CategoryID);
    }

    [Fact]
    public async Task AddCategory_DuplicateName_Returns409()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);
        var payload = new { categoryName = "Duplicate", distanceKm = 5, entryFee = 50 };

        var first = await organiser.PostAsJsonAsync($"/api/events/{ev.EventID}/categories", payload);
        var second = await organiser.PostAsJsonAsync($"/api/events/{ev.EventID}/categories", payload);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateAndDeleteCategory()
    {
        var organiser = await LoginOrganiserAsync();
        var ev = await CreateEventAsync(organiser);
        var category = await AddCategoryAsync(organiser, ev.EventID);

        var update = await organiser.PutAsJsonAsync($"/api/categories/{category.CategoryID}",
            new { categoryName = "Renamed Category", distanceKm = 21.1, entryFee = 200, maxEntrants = 50 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await organiser.DeleteAsync($"/api/categories/{category.CategoryID}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task GetRoutes_ForSeededEvent_ReturnsRoute()
    {
        var all = await NewClient().GetFromJsonAsync<List<EventDto>>("/api/events");
        var cycle = all!.First(e => e.EventName == "Cape Town Cycle Tour");

        var routes = await NewClient().GetFromJsonAsync<List<RouteDto>>($"/api/events/{cycle.EventID}/routes");
        Assert.Single(routes!);
    }

    [Fact]
    public async Task Organiser_GetMine_OnlyReturnsOwnEvents()
    {
        var organiser = await LoginSecondOrganiserAsync(); // Sarah owns the Cape Town Cycle Tour only
        var mine = await organiser.GetFromJsonAsync<List<EventDto>>("/api/events/mine");

        Assert.NotEmpty(mine!);
        Assert.All(mine!, e => Assert.Equal("Sarah van der Merwe", e.OrganiserName));
    }
}
