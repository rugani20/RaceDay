using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

// ---------- Auth / users ----------
public class RegisterRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be 'Organiser' or 'Participant'.")]
    public string Role { get; set; } = string.Empty;
    [StringLength(20)] public string? PhoneNumber { get; set; }
    [StringLength(50)] public string? City { get; set; }
    /// <summary>Required only when Role is Organiser.</summary>
    public string? OrganiserCode { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [StringLength(20)] public string? PhoneNumber { get; set; }
    [StringLength(50)] public string? City { get; set; }
}

public record UserDto(int UserID, string FullName, string Email, string Role, string? PhoneNumber, string? City);
public record LoginResponse(string Message, UserDto User);

// ---------- Events ----------
public class EventRequest
{
    [Required, StringLength(150)] public string EventName { get; set; } = string.Empty;
    [Required, RegularExpression("^(Running|Walking|Cycling)$", ErrorMessage = "EventType must be Running, Walking or Cycling.")]
    public string EventType { get; set; } = string.Empty;
    [Required] public DateOnly? EventDate { get; set; }
    [Required, StringLength(100)] public string Location { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Range(1, int.MaxValue)] public int? MaxParticipants { get; set; }
    /// <summary>Optional. Upcoming, Completed or Cancelled (used when updating).</summary>
    [RegularExpression("^(Upcoming|Completed|Cancelled)$", ErrorMessage = "Status must be Upcoming, Completed or Cancelled.")]
    public string? Status { get; set; }
}

public record EventDto(int EventID, int OrganiserID, string OrganiserName, string EventName, string EventType,
    DateOnly EventDate, string Location, string? Description, int? MaxParticipants, string Status);

public record EventDetailDto(int EventID, int OrganiserID, string OrganiserName, string EventName, string EventType,
    DateOnly EventDate, string Location, string? Description, int? MaxParticipants, string Status,
    List<CategoryDto> Categories, List<RouteDto> Routes);

// ---------- Categories / routes ----------
public class CategoryRequest
{
    [Required, StringLength(100)] public string CategoryName { get; set; } = string.Empty;
    [Range(0.01, 999.99)] public decimal DistanceKm { get; set; }
    [Range(0, 999999.99)] public decimal EntryFee { get; set; }
    [Range(1, int.MaxValue)] public int? MaxEntrants { get; set; }
}

public record CategoryDto(int CategoryID, int EventID, string CategoryName, decimal DistanceKm, decimal EntryFee, int? MaxEntrants);
public record RouteDto(int RouteID, int EventID, string RouteName, decimal DistanceKm, int? ElevationGainM, string? MapUrl);

// ---------- Enrolments ----------
public class EnrolRequest
{
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}

public record EnrolmentDto(int EnrolmentID, int ParticipantID, string ParticipantName, int EventID, string EventName,
    DateOnly EventDate, string EventType, int CategoryID, string CategoryName, DateTime EnrolmentDate, string Status,
    ResultDto? Result);

// ---------- Results ----------
public class ResultDetails
{
    /// <summary>Finish time as hh:mm:ss, e.g. 03:45:12. Required when Status is Finished.</summary>
    public TimeSpan? FinishTime { get; set; }
    /// <summary>Finishing position (1 = winner). Required when Status is Finished.</summary>
    [Range(1, int.MaxValue)] public int? Position { get; set; }
    /// <summary>Finished, DNF or DQ.</summary>
    [RegularExpression("^(Finished|DNF|DQ)$", ErrorMessage = "Status must be Finished, DNF or DQ.")]
    public string Status { get; set; } = "Finished";
}

public class ResultRequest : ResultDetails
{
    [Range(1, int.MaxValue)] public int EnrolmentId { get; set; }
}

public class UpdateResultRequest : ResultDetails { }

public record ResultDto(int ResultID, int EnrolmentID, TimeSpan? FinishTime, int? Position, string Status);
