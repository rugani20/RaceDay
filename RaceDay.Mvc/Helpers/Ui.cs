namespace RaceDay.Mvc.Helpers;
public static class Ui
{
    public static string StatusClass(string? s) => (s ?? "").ToLower() switch
    {
        "confirmed" => "status-confirmed",
        "pending" => "status-pending",
        "cancelled" or "rejected" => "status-cancelled",
        _ => "status-other"
    };
    public static string Ordinal(int n) => n % 100 is 11 or 12 or 13 ? $"{n}th" : (n % 10) switch { 1 => $"{n}st", 2 => $"{n}nd", 3 => $"{n}rd", _ => $"{n}th" };
}
