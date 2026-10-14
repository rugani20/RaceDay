using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public EventsController(AppDbContext db) => _db = db;

    // ======================= Events =======================

    /// <summary>Lists upcoming events (public). Supports searching and filtering.</summary>
    /// <param name="search">Optional text matched against event name and location.</param>
    /// <param name="eventType">Optional filter: Running, Walking or Cycling.</param>
    /// <param name="includePast">Set true to include past and cancelled events.</param>
    /// <response code="200">Array of events ordered by date.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents(
        [FromQuery] string? search, [FromQuery] string? eventType, [FromQuery] bool includePast = false)
    {
        IQueryable<Event> query = _db.Events.Include(e => e.Organiser);

        if (!includePast)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            query = query.Where(e => e.EventDate >= today && e.Status != EventStatus.Cancelled);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.EventName.Contains(term) || e.Location.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var type = eventType.Trim();
            query = query.Where(e => e.EventType == type);
        }

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(e => e.ToDto()));
    }

    /// <summary>Lists every event created by the logged-in Organiser, including past events (Organiser only).</summary>
    /// <response code="200">Array of the organiser's events.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpGet("mine")]
    [SessionAuthorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetMyEvents()
    {
        var userId = CurrentUserId;
        var events = await _db.Events.Include(e => e.Organiser)
            .Where(e => e.OrganiserID == userId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();
        return Ok(events.Select(e => e.ToDto()));
    }

    /// <summary>Returns one event with its categories and routes (public).</summary>
    /// <response code="200">Event details.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> GetEvent(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Organiser).Include(e => e.Categories).Include(e => e.Routes)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev is null) return NotFoundMessage("Event not found.");
        return Ok(ev.ToDetailDto());
    }

    /// <summary>Creates a new event owned by the logged-in Organiser (Organiser only).</summary>
    /// <remarks>Send eventName, eventType (Running/Walking/Cycling), eventDate (yyyy-MM-dd), location, optional description and maxParticipants.</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpPost]
    [SessionAuthorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDto>> CreateEvent(EventRequest request)
    {
        var ev = new Event
        {
            OrganiserID = CurrentUserId,
            EventName = request.EventName.Trim(),
            EventType = request.EventType,
            EventDate = request.EventDate!.Value,
            Location = request.Location.Trim(),
            Description = request.Description?.Trim(),
            MaxParticipants = request.MaxParticipants,
            Status = EventStatus.Upcoming
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return Created($"/api/events/{ev.EventID}", ev.ToDto());
    }

    /// <summary>Updates an event owned by the logged-in Organiser (Organiser only).</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> UpdateEvent(int id, EventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFoundMessage("Event not found.");
        if (ev.OrganiserID != CurrentUserId) return Forbidden("You can only change your own events.");

        ev.EventName = request.EventName.Trim();
        ev.EventType = request.EventType;
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.Description = request.Description?.Trim();
        ev.MaxParticipants = request.MaxParticipants;
        if (!string.IsNullOrEmpty(request.Status)) ev.Status = request.Status;

        await _db.SaveChangesAsync();
        return Ok(ev.ToDto());
    }

    /// <summary>Deletes an event owned by the logged-in Organiser, together with its categories, routes, enrolments and results (Organiser only).</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFoundMessage("Event not found.");
        if (ev.OrganiserID != CurrentUserId) return Forbidden("You can only delete your own events.");

        // Foreign keys use NO ACTION (matches the Part 1 script), so remove children first.
        var categories = await _db.Categories.Where(c => c.EventID == id).ToListAsync();
        var categoryIds = categories.Select(c => c.CategoryID).ToList();
        var enrolments = await _db.Enrolments.Include(en => en.Result)
            .Where(en => categoryIds.Contains(en.CategoryID)).ToListAsync();
        var routes = await _db.Routes.Where(r => r.EventID == id).ToListAsync();

        _db.Results.RemoveRange(enrolments.Where(en => en.Result != null).Select(en => en.Result!));
        _db.Enrolments.RemoveRange(enrolments);
        _db.Categories.RemoveRange(categories);
        _db.Routes.RemoveRange(routes);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ======================= Categories (per event) =======================

    /// <summary>Lists the categories of an event (public).</summary>
    /// <response code="200">Array of categories.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == id)) return NotFoundMessage("Event not found.");

        var categories = await _db.Categories.Where(c => c.EventID == id).OrderBy(c => c.DistanceKm).ToListAsync();
        return Ok(categories.Select(c => c.ToDto()));
    }

    /// <summary>Adds an age or distance category to an event owned by the logged-in Organiser (Organiser only).</summary>
    /// <remarks>Send categoryName, distanceKm, entryFee and optional maxEntrants.</remarks>
    /// <response code="201">Category created.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="409">The event already has a category with this name.</response>
    [HttpPost("{id:int}/categories")]
    [SessionAuthorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> AddCategory(int id, CategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFoundMessage("Event not found.");
        if (ev.OrganiserID != CurrentUserId) return Forbidden("You can only add categories to your own events.");

        var name = request.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventID == id && c.CategoryName == name))
            return Conflict(new ErrorResponse("This event already has a category with that name."));

        var category = new Category
        {
            EventID = id,
            CategoryName = name,
            DistanceKm = request.DistanceKm,
            EntryFee = request.EntryFee,
            MaxEntrants = request.MaxEntrants
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{id}/categories", category.ToDto());
    }

    // ======================= Routes =======================

    /// <summary>Lists the route information (distance, elevation, map link) for an event (public).</summary>
    /// <response code="200">Array of routes.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id:int}/routes")]
    [ProducesResponseType(typeof(IEnumerable<RouteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<RouteDto>>> GetRoutes(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == id)) return NotFoundMessage("Event not found.");

        var routes = await _db.Routes.Where(r => r.EventID == id).ToListAsync();
        return Ok(routes.Select(r => r.ToDto()));
    }

    // ======================= Enrolments (organiser view) =======================

    /// <summary>Lists all enrolments for an event owned by the logged-in Organiser, including any captured results (Organiser only).</summary>
    /// <response code="200">Array of enrolments.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id:int}/enrolments")]
    [SessionAuthorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetEventEnrolments(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFoundMessage("Event not found.");
        if (ev.OrganiserID != CurrentUserId) return Forbidden("You can only view enrolments for your own events.");

        var enrolments = await _db.Enrolments.WithDetails()
            .Where(en => en.Category.EventID == id)
            .OrderBy(en => en.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(en => en.ToDto()));
    }
}
