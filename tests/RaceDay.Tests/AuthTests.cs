using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class AuthTests : ApiTestBase
{
    public AuthTests(RaceDayFactory factory) : base(factory) { }

    // ---------- Registration ----------
    [Fact]
    public async Task Register_ValidParticipant_Returns201_AndNeverExposesThePassword()
    {
        var response = await NewClient().PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "New Runner", email = UniqueEmail(), password = "Password123!", role = "Participant", city = "Durban"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Participant", body);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = UniqueEmail();
        var payload = new { fullName = "Dup User", email, password = "Password123!", role = "Participant" };

        var first = await NewClient().PostAsJsonAsync("/api/auth/register", payload);
        var second = await NewClient().PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var response = await NewClient().PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Bad Email", email = "not-an-email", password = "Password123!", role = "Participant"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var response = await NewClient().PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Short Pwd", email = UniqueEmail(), password = "123", role = "Participant"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var response = await NewClient().PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Bad Role", email = UniqueEmail(), password = "Password123!", role = "Admin"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithoutCode_Returns403()
    {
        var response = await NewClient().PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Sneaky Organiser", email = UniqueEmail(), password = "Password123!", role = "Organiser"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithCode_Returns201()
    {
        var response = await NewClient().PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Real Organiser", email = UniqueEmail(), password = "Password123!",
            role = "Organiser", organiserCode = OrganiserCode
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ---------- Login / session ----------
    [Fact]
    public async Task Login_ValidCredentials_Returns200_AndStartsSession()
    {
        var client = NewClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = OrganiserEmail, password = DbSeeder.DemoPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("Organiser", body!.User.Role);

        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await NewClient().PostAsJsonAsync("/api/auth/login", new { email = OrganiserEmail, password = "WrongPassword!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await NewClient().PostAsJsonAsync("/api/auth/login", new { email = UniqueEmail(), password = "Password123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ClearsSession_SoProfileIsThenUnauthorised()
    {
        var client = await RegisterParticipantAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    // ---------- Profile ----------
    [Fact]
    public async Task Profile_WithoutLogin_Returns401()
    {
        var response = await NewClient().GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_UpdateOwnProfile_Returns200_AndPersists()
    {
        var client = await RegisterParticipantAsync();

        var update = await client.PutAsJsonAsync("/api/users/me", new { fullName = "Renamed Runner", phoneNumber = "0799999999", city = "Gqeberha" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var me = await client.GetFromJsonAsync<UserDto>("/api/users/me");
        Assert.Equal("Renamed Runner", me!.FullName);
        Assert.Equal("Gqeberha", me.City);
    }
}
