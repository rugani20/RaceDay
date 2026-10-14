using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>Registers a new Organiser or Participant account.</summary>
    /// <remarks>
    /// Send fullName, email, password (min 8 characters), role ("Organiser" or "Participant"), and optionally phoneNumber and city.
    /// Organiser accounts must also send the secret organiserCode so that not everyone can become an Organiser.
    /// The password is hashed (PBKDF2) and never returned.
    /// </remarks>
    /// <response code="201">Account created. Returns the user profile (no password).</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="403">Organiser registration code missing or wrong.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest request)
    {
        if (request.Role == AppRoles.Organiser)
        {
            var expected = _config["Registration:OrganiserCode"];
            if (string.IsNullOrEmpty(expected) || request.OrganiserCode != expected)
                return Forbidden("A valid organiser registration code is required to register as an Organiser.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new ErrorResponse("This email address is already registered."));

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordService.Hash(request.Password),
            Role = request.Role,
            PhoneNumber = request.PhoneNumber?.Trim(),
            City = request.City?.Trim()
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created("/api/users/me", user.ToDto());
    }

    /// <summary>Logs a user in and starts a server-side session.</summary>
    /// <remarks>Send email and password. On success a session cookie is set; the session stores the user id and role for all later requests.</remarks>
    /// <response code="200">Logged in. Returns the user profile.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !PasswordService.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new ErrorResponse("Invalid email or password."));

        HttpContext.Session.Clear(); // fresh session on every login
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserID);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(new LoginResponse("Login successful.", user.ToDto()));
    }

    /// <summary>Logs the current user out and clears the session.</summary>
    /// <response code="200">Session cleared.</response>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public ActionResult<MessageResponse> Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new MessageResponse("Logged out."));
    }
}
