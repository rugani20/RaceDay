namespace RaceDay.Api.Models;

public static class AppRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class EventStatus
{
    public const string Upcoming = "Upcoming";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

public static class EnrolmentStatus
{
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
}

public static class ResultStatus
{
    public const string Finished = "Finished";
    public const string Dnf = "DNF";
    public const string Dq = "DQ";
}
