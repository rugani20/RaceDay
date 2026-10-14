namespace RaceDay.Api.Dtos;

/// <summary>Standard error body returned for 401/403/404/409 and business-rule 400 responses.</summary>
public record ErrorResponse(string Message);

/// <summary>Simple confirmation body.</summary>
public record MessageResponse(string Message);
