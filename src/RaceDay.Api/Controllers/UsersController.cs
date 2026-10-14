using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
[SessionAuthorize]
public class UsersController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    /// <summary>Returns the logged-in user's profile.</summary>
    /// <response code="200">The user profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var userId = CurrentUserId;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new ErrorResponse("Your account no longer exists."));
        }
        return Ok(user.ToDto());
    }

    /// <summary>Updates the logged-in user's full name, phone number and city.</summary>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest request)
    {
        var userId = CurrentUserId;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new ErrorResponse("Your account no longer exists."));
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.City = request.City?.Trim();
        await _db.SaveChangesAsync();

        return Ok(user.ToDto());
    }
}
