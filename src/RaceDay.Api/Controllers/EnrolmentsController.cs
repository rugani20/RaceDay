using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/enrolments")]
[Produces("application/json")]
[SessionAuthorize(Roles = AppRoles.Participant)]
public class EnrolmentsController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public EnrolmentsController(AppDbContext db) => _db = db;

    /// <summary>Enrols the logged-in Participant in an event category (Participant only).</summary>
    /// <remarks>Send { "categoryId": 1 }. The link between participant, event and category is recorded immediately.</remarks>
    /// <response code="201">Enrolment created.</response>
    /// <response code="400">Invalid input, or the event is not open for enrolment.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    /// <response code="404">Category does not exist.</response>
    /// <response code="409">Already enrolled, or the category/event is full.</response>
    [HttpPost]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Enrol(EnrolRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == request.CategoryId);
        if (category is null) return NotFoundMessage("Category not found.");

        if (category.Event.Status != EventStatus.Upcoming)
            return BadRequest(new ErrorResponse("This event is not open for enrolment."));

        var userId = CurrentUserId;
        var existing = await _db.Enrolments.FirstOrDefaultAsync(e => e.ParticipantID == userId && e.CategoryID == category.CategoryID);
        if (existing is not null && existing.Status == EnrolmentStatus.Confirmed)
            return Conflict(new ErrorResponse("You are already enrolled in this category."));

        if (category.MaxEntrants.HasValue)
        {
            var inCategory = await _db.Enrolments.CountAsync(e => e.CategoryID == category.CategoryID && e.Status == EnrolmentStatus.Confirmed);
            if (inCategory >= category.MaxEntrants.Value)
                return Conflict(new ErrorResponse("This category is full."));
        }

        if (category.Event.MaxParticipants.HasValue)
        {
            var eventId = category.EventID;
            var inEvent = await _db.Enrolments.CountAsync(e => e.Category.EventID == eventId && e.Status == EnrolmentStatus.Confirmed);
            if (inEvent >= category.Event.MaxParticipants.Value)
                return Conflict(new ErrorResponse("This event is full."));
        }

        Enrolment enrolment;
        if (existing is not null) // re-enrolling after a cancellation
        {
            existing.Status = EnrolmentStatus.Confirmed;
            existing.EnrolmentDate = DateTime.Now;
            enrolment = existing;
        }
        else
        {
            enrolment = new Enrolment { ParticipantID = userId, CategoryID = category.CategoryID, Status = EnrolmentStatus.Confirmed };
            _db.Enrolments.Add(enrolment);
        }

        await _db.SaveChangesAsync();

        var enrolmentId = enrolment.EnrolmentID;
        var full = await _db.Enrolments.WithDetails().FirstAsync(e => e.EnrolmentID == enrolmentId);
        return Created($"/api/enrolments/{enrolmentId}", full.ToDto());
    }

    /// <summary>Lists the logged-in Participant's own enrolments with any results (performance history) (Participant only).</summary>
    /// <response code="200">Array of enrolments, newest event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetMine()
    {
        var userId = CurrentUserId;
        var enrolments = await _db.Enrolments.WithDetails()
            .Where(e => e.ParticipantID == userId)
            .OrderByDescending(e => e.Category.Event.EventDate)
            .ToListAsync();

        return Ok(enrolments.Select(e => e.ToDto()));
    }

    /// <summary>Cancels the logged-in Participant's own enrolment (status becomes Cancelled) (Participant only).</summary>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="403">Not a Participant, or the enrolment belongs to someone else.</response>
    /// <response code="404">Enrolment does not exist.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (enrolment is null) return NotFoundMessage("Enrolment not found.");
        if (enrolment.ParticipantID != CurrentUserId) return Forbidden("You can only cancel your own enrolments.");

        if (await _db.Results.AnyAsync(r => r.EnrolmentID == id))
            return Conflict(new ErrorResponse("A result has been captured for this enrolment, so it cannot be cancelled."));

        enrolment.Status = EnrolmentStatus.Cancelled;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
