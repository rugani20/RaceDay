using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/results")]
[Produces("application/json")]
public class ResultsController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public ResultsController(AppDbContext db) => _db = db;

    private static string? Validate(ResultDetails details)
    {
        if (details.Status == ResultStatus.Finished && (details.FinishTime is null || details.Position is null))
            return "A Finished result needs both a finishTime and a position.";
        return null;
    }

    /// <summary>Captures a participant's finish time and position for an enrolment (Organiser only).</summary>
    /// <remarks>Send enrolmentId, finishTime (hh:mm:ss), position and status (Finished, DNF or DQ). The enrolment must belong to one of the Organiser's events.</remarks>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Invalid input, or the enrolment is cancelled.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Enrolment does not exist.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost]
    [SessionAuthorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> Capture(ResultRequest request)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentID == request.EnrolmentId);
        if (enrolment is null) return NotFoundMessage("Enrolment not found.");
        if (enrolment.Category.Event.OrganiserID != CurrentUserId)
            return Forbidden("You can only capture results for your own events.");
        if (enrolment.Status != EnrolmentStatus.Confirmed)
            return BadRequest(new ErrorResponse("Results can only be captured for confirmed enrolments."));
        if (await _db.Results.AnyAsync(r => r.EnrolmentID == request.EnrolmentId))
            return Conflict(new ErrorResponse("A result already exists for this enrolment. Use PUT to update it."));

        var error = Validate(request);
        if (error is not null) return BadRequest(new ErrorResponse(error));

        var result = new Result
        {
            EnrolmentID = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = request.Status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return Created($"/api/results/{result.ResultID}", result.ToDto());
    }

    /// <summary>Updates a captured result (Organiser only).</summary>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Result does not exist.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> Update(int id, UpdateResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.ResultID == id);
        if (result is null) return NotFoundMessage("Result not found.");
        if (result.Enrolment.Category.Event.OrganiserID != CurrentUserId)
            return Forbidden("You can only change results for your own events.");

        var error = Validate(request);
        if (error is not null) return BadRequest(new ErrorResponse(error));

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = request.Status;
        await _db.SaveChangesAsync();

        return Ok(result.ToDto());
    }

    /// <summary>Returns a single result. Allowed for the participant it belongs to and the Organiser of that event (any logged-in user, ownership enforced).</summary>
    /// <response code="200">The result.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The result belongs to someone else's enrolment or another Organiser's event.</response>
    /// <response code="404">Result does not exist.</response>
    [HttpGet("{id:int}")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> Get(int id)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.ResultID == id);
        if (result is null) return NotFoundMessage("Result not found.");

        var userId = CurrentUserId;
        var isOwnerParticipant = result.Enrolment.ParticipantID == userId;
        var isEventOrganiser = result.Enrolment.Category.Event.OrganiserID == userId;
        if (!isOwnerParticipant && !isEventOrganiser)
            return Forbidden("You are not allowed to view this result.");

        return Ok(result.ToDto());
    }
}
