using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Data;

/// <summary>Seeds the same sample data as the Part 1 script (with real password hashes). Runs only on an empty database.</summary>
public static class DbSeeder
{
    public const string DemoPassword = "Password123!";

    public static void Seed(AppDbContext db)
    {
        if (db.Users.Any()) return;

        var thabo = new User { FullName = "Thabo Nkosi", Email = "thabo.nkosi@raceday-events.co.za", PasswordHash = PasswordService.Hash(DemoPassword), Role = AppRoles.Organiser, PhoneNumber = "0821234567", City = "Johannesburg" };
        var sarah = new User { FullName = "Sarah van der Merwe", Email = "sarah.vdm@raceday-events.co.za", PasswordHash = PasswordService.Hash(DemoPassword), Role = AppRoles.Organiser, PhoneNumber = "0834567890", City = "Cape Town" };
        var lindiwe = new User { FullName = "Lindiwe Dlamini", Email = "lindiwe.dlamini@example.co.za", PasswordHash = PasswordService.Hash(DemoPassword), Role = AppRoles.Participant, PhoneNumber = "0712345678", City = "Soweto" };
        var johan = new User { FullName = "Johan Botha", Email = "johan.botha@example.co.za", PasswordHash = PasswordService.Hash(DemoPassword), Role = AppRoles.Participant, PhoneNumber = "0723456789", City = "Pretoria" };

        // Dates are relative to today so the sample events always make sense.
        var today = DateOnly.FromDateTime(DateTime.Today);

        var soweto = new Event
        {
            Organiser = thabo, EventName = "Soweto Marathon", EventType = "Running", EventDate = today.AddDays(-30),
            Location = "Soweto", Description = "Annual road running event through the streets of Soweto.",
            MaxParticipants = 5000, Status = EventStatus.Completed,
            Categories =
            {
                new Category { CategoryName = "10km Individual", DistanceKm = 10.00m, EntryFee = 150.00m, MaxEntrants = 2000 },
                new Category { CategoryName = "42km Full Marathon", DistanceKm = 42.20m, EntryFee = 350.00m, MaxEntrants = 1500 }
            },
            Routes = { new RaceRoute { RouteName = "Soweto Marathon Main Route", DistanceKm = 42.20m, ElevationGainM = 320, MapUrl = "https://maps.example.co.za/soweto-marathon" } }
        };

        var cycle = new Event
        {
            Organiser = sarah, EventName = "Cape Town Cycle Tour", EventType = "Cycling", EventDate = today.AddDays(20),
            Location = "Cape Town", Description = "Scenic cycling tour around the Cape Peninsula.",
            MaxParticipants = 8000, Status = EventStatus.Upcoming,
            Categories =
            {
                new Category { CategoryName = "109km Cycle Tour", DistanceKm = 109.00m, EntryFee = 550.00m, MaxEntrants = 6000 },
                new Category { CategoryName = "35km Mini Tour", DistanceKm = 35.00m, EntryFee = 300.00m, MaxEntrants = 2000 }
            },
            Routes = { new RaceRoute { RouteName = "Cape Peninsula Loop", DistanceKm = 109.00m, ElevationGainM = 980, MapUrl = "https://maps.example.co.za/cape-cycle-tour" } }
        };

        var walk = new Event
        {
            Organiser = thabo, EventName = "Pretoria Fun Walk", EventType = "Walking", EventDate = today.AddDays(45),
            Location = "Pretoria", Description = "Community fun walk in support of local charities.",
            MaxParticipants = 2000, Status = EventStatus.Upcoming,
            Categories = { new Category { CategoryName = "5km Fun Walk", DistanceKm = 5.00m, EntryFee = 50.00m, MaxEntrants = 2000 } },
            Routes = { new RaceRoute { RouteName = "Pretoria CBD Loop", DistanceKm = 5.00m, ElevationGainM = 40, MapUrl = "https://maps.example.co.za/pretoria-fun-walk" } }
        };

        var enrolments = new[]
        {
            new Enrolment
            {
                Participant = lindiwe, Category = soweto.Categories.First(c => c.CategoryName.StartsWith("42km")),
                Status = EnrolmentStatus.Confirmed,
                Result = new Result { FinishTime = new TimeSpan(3, 45, 12), Position = 152, Status = ResultStatus.Finished }
            },
            new Enrolment { Participant = johan, Category = cycle.Categories.First(c => c.CategoryName.StartsWith("109km")), Status = EnrolmentStatus.Confirmed },
            new Enrolment { Participant = lindiwe, Category = walk.Categories.First(), Status = EnrolmentStatus.Confirmed }
        };

        db.Users.AddRange(thabo, sarah, lindiwe, johan);
        db.Events.AddRange(soweto, cycle, walk);
        db.Enrolments.AddRange(enrolments);
        db.SaveChanges();
    }
}
