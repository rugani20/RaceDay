using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public abstract class ApiTestBase : IClassFixture<RaceDayFactory>
{
    protected const string OrganiserEmail = "thabo.nkosi@raceday-events.co.za";
    protected const string SecondOrganiserEmail = "sarah.vdm@raceday-events.co.za";

    protected RaceDayFactory Factory { get; }

    protected ApiTestBase(RaceDayFactory factory) => Factory = factory;

    protected string OrganiserCode =>
        Factory.Services.GetRequiredService<IConfiguration>()["Registration:OrganiserCode"]!;

    /// <summary>A new client with its own cookie container (= its own session).</summary>
    protected HttpClient NewClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    protected static string UniqueEmail() => $"user{Guid.NewGuid():N}@example.co.za";

    protected async Task<HttpClient> LoginAsync(string email, string password = DbSeeder.DemoPassword)
    {
        var client = NewClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return client;
    }

    protected Task<HttpClient> LoginOrganiserAsync() => LoginAsync(OrganiserEmail);
    protected Task<HttpClient> LoginSecondOrganiserAsync() => LoginAsync(SecondOrganiserEmail);

    /// <summary>Registers a brand-new Participant and returns a client that is already logged in as them.</summary>
    protected async Task<HttpClient> RegisterParticipantAsync()
    {
        var client = NewClient();
        var email = UniqueEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test Participant", email, password = DbSeeder.DemoPassword,
            role = "Participant", phoneNumber = "0710000000", city = "Durban"
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = DbSeeder.DemoPassword });
        login.EnsureSuccessStatusCode();
        return client;
    }

    protected static object NewEventPayload(string name = "Test Event") => new
    {
        eventName = name, eventType = "Running", eventDate = "2027-05-01",
        location = "Durban", description = "Created by a unit test", maxParticipants = 500
    };

    protected async Task<EventDto> CreateEventAsync(HttpClient organiser, string name = "Test Event")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEventPayload(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    protected async Task<CategoryDto> AddCategoryAsync(HttpClient organiser, int eventId, int? maxEntrants = null)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Cat-" + Guid.NewGuid().ToString("N")[..8],
            distanceKm = 10, entryFee = 100, maxEntrants
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }
}
