using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Security;

/// <summary>
/// Session-based authorisation. 401 when nobody is logged in, 403 when the logged-in role is not allowed.
/// Usage: [SessionAuthorize] (any logged-in user) or [SessionAuthorize(Roles = AppRoles.Organiser)].
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>Comma separated list of allowed roles. Leave empty to allow any logged-in user.</summary>
    public string? Roles { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);

        if (userId is null)
        {
            context.Result = new UnauthorizedObjectResult(new ErrorResponse("You must be logged in to access this resource."));
            return;
        }

        if (!string.IsNullOrWhiteSpace(Roles))
        {
            var role = session.GetString(SessionKeys.Role);
            var allowed = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (role is null || !allowed.Contains(role))
            {
                context.Result = new ObjectResult(new ErrorResponse("Your role is not permitted to perform this action."))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
