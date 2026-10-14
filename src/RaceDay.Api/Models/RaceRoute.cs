namespace RaceDay.Api.Models;

/// <summary>Maps to the "Routes" table from the Part 1 ERD (class named RaceRoute to avoid clashing with MVC's [Route]).</summary>
public class RaceRoute
{
    public int RouteID { get; set; }
    public int EventID { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int? ElevationGainM { get; set; }
    public string? MapUrl { get; set; }

    public Event Event { get; set; } = null!;
}
