using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Entity -> DTO mapping. DTOs keep password hashes and circular references out of API responses.</summary>
public static class Mapper
{
    public static UserDto ToDto(this User u) =>
        new(u.UserID, u.FullName, u.Email, u.Role, u.PhoneNumber, u.City);

    public static EventDto ToDto(this Event e) =>
        new(e.EventID, e.OrganiserID, e.Organiser?.FullName ?? string.Empty, e.EventName, e.EventType,
            e.EventDate, e.Location, e.Description, e.MaxParticipants, e.Status);

    public static EventDetailDto ToDetailDto(this Event e) =>
        new(e.EventID, e.OrganiserID, e.Organiser?.FullName ?? string.Empty, e.EventName, e.EventType,
            e.EventDate, e.Location, e.Description, e.MaxParticipants, e.Status,
            e.Categories.OrderBy(c => c.DistanceKm).Select(c => c.ToDto()).ToList(),
            e.Routes.Select(r => r.ToDto()).ToList());

    public static CategoryDto ToDto(this Category c) =>
        new(c.CategoryID, c.EventID, c.CategoryName, c.DistanceKm, c.EntryFee, c.MaxEntrants);

    public static RouteDto ToDto(this RaceRoute r) =>
        new(r.RouteID, r.EventID, r.RouteName, r.DistanceKm, r.ElevationGainM, r.MapUrl);

    public static ResultDto ToDto(this Result r) =>
        new(r.ResultID, r.EnrolmentID, r.FinishTime, r.Position, r.Status);

    /// <summary>Requires Participant, Category.Event and Result to be loaded - use WithDetails().</summary>
    public static EnrolmentDto ToDto(this Enrolment e) =>
        new(e.EnrolmentID, e.ParticipantID, e.Participant.FullName, e.Category.EventID, e.Category.Event.EventName,
            e.Category.Event.EventDate, e.Category.Event.EventType, e.CategoryID, e.Category.CategoryName,
            e.EnrolmentDate, e.Status, e.Result?.ToDto());

    public static IQueryable<Enrolment> WithDetails(this IQueryable<Enrolment> query) =>
        query.Include(e => e.Participant)
             .Include(e => e.Category).ThenInclude(c => c.Event)
             .Include(e => e.Result);
}
