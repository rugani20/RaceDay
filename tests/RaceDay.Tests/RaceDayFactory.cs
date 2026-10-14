using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RaceDay.Tests;

/// <summary>
/// Starts the real API pipeline (controllers, session, role filters) in-memory.
/// The "Testing" environment makes Program.cs use an in-memory database seeded with the demo data.
/// </summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
