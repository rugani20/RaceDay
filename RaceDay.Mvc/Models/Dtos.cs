using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
namespace RaceDay.Mvc.Models;

public class LoginResponse { public LoginUser? User { get; set; } }

public class LoginUser { public int UserID { get; set; } public string FullName { get; set; } = ""; public string Email { get; set; } = ""; public string Role { get; set; } = ""; }


public class EventDto { [JsonPropertyName("eventID")] public int Id { get; set; } [JsonPropertyName("eventName")] public string Name { get; set; } = ""; public string Description { get; set; } = ""; public DateTime EventDate { get; set; } public string Location { get; set; } = ""; public decimal DistanceKm { get; set; }
    public class CatInfo { public decimal DistanceKm { get; set; } }
    public List<CatInfo> Categories { get; set; } = new();
    public string EventType { get; set; } = ""; public string? BannerUrl { get; set; } public int OrganiserId { get; set; } public int EnrolmentCount { get; set; } public int? MaxParticipants { get; set; } public string Status { get; set; } = ""; }


public class CategoryDto { public int Id { get; set; } public int EventId { get; set; } public string Name { get; set; } = ""; public string CategoryType { get; set; } = ""; }
public class EnrolmentDto { public int Id { get; set; } public int EventId { get; set; } public string EventName { get; set; } = ""; public DateTime EventDate { get; set; } public int UserId { get; set; } public string ParticipantName { get; set; } = ""; public int CategoryId { get; set; } public string CategoryName { get; set; } = ""; public string Status { get; set; } = ""; public DateTime EnrolledAt { get; set; } }
public class ResultDto { public int Id { get; set; } public int EnrolmentId { get; set; } public string EventName { get; set; } = ""; public DateTime EventDate { get; set; } public string CategoryName { get; set; } = ""; public string FinishTime { get; set; } = ""; public int Position { get; set; } public int TotalFinishers { get; set; } }
public class UserDto { public int Id { get; set; } public string FullName { get; set; } = ""; public string Email { get; set; } = ""; public string? Phone { get; set; } public string? ProfilePictureUrl { get; set; } }

public class LoginVm { [Required, EmailAddress] public string Email { get; set; } = ""; [Required, DataType(DataType.Password)] public string Password { get; set; } = ""; }
public class RegisterVm
{

    [Required, Display(Name = "Full name")] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Phone] public string? Phone { get; set; }
    public string? City { get; set; }

    [Display(Name = "Organiser code")] public string? OrganiserCode { get; set; }


    [Required, MinLength(6), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Confirm password")] public string ConfirmPassword { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
}
public class EventFormVm
{
    public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
    [Required] public string Description { get; set; } = "";
    [Required, DataType(DataType.Date), Display(Name = "Event date")] public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);
    [Required] public string Location { get; set; } = "";
    [Required, Range(0.1, 1000), Display(Name = "Distance (km)")] public decimal DistanceKm { get; set; }
    [Required, Display(Name = "Event type")] public string EventType { get; set; } = "Road Run";
    public IFormFile? Banner { get; set; }
    public string? ExistingBannerUrl { get; set; }
}
public class CategoryFormVm { public int EventId { get; set; } [Required] public string Name { get; set; } = ""; [Required] public string CategoryType { get; set; } = "Age"; }
public class ResultEntryVm { public int EnrolmentId { get; set; } public string ParticipantName { get; set; } = ""; public string CategoryName { get; set; } = ""; public string? FinishTime { get; set; } public int? Position { get; set; } }
public class ProfileVm { [Required, Display(Name = "Full name")] public string FullName { get; set; } = ""; [EmailAddress] public string Email { get; set; } = ""; [Phone] public string? Phone { get; set; } public string? ProfilePictureUrl { get; set; } }
