namespace RaceDay.Api.Models;

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? MaxParticipants { get; set; }
    public string Status { get; set; } = EventStatus.Upcoming;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public User Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<RaceRoute> Routes { get; set; } = new List<RaceRoute>();
}
