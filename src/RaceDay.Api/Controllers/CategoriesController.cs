using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
[SessionAuthorize(Roles = AppRoles.Organiser)]
public class CategoriesController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    /// <summary>Updates a category of an event owned by the logged-in Organiser (Organiser only).</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="403">Not an Organiser, or the category's event belongs to another Organiser.</response>
    /// <response code="404">Category does not exist.</response>
    /// <response code="409">Another category of this event already uses that name.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null) return NotFoundMessage("Category not found.");
        if (category.Event.OrganiserID != CurrentUserId) return Forbidden("You can only change categories of your own events.");

        var name = request.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventID == category.EventID && c.CategoryName == name && c.CategoryID != id))
            return Conflict(new ErrorResponse("This event already has a category with that name."));

        category.CategoryName = name;
        category.DistanceKm = request.DistanceKm;
        category.EntryFee = request.EntryFee;
        category.MaxEntrants = request.MaxEntrants;
        await _db.SaveChangesAsync();

        return Ok(category.ToDto());
    }

    /// <summary>Deletes a category of an event owned by the logged-in Organiser (Organiser only).</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="403">Not an Organiser, or the category's event belongs to another Organiser.</response>
    /// <response code="404">Category does not exist.</response>
    /// <response code="409">Participants are already enrolled in this category.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null) return NotFoundMessage("Category not found.");
        if (category.Event.OrganiserID != CurrentUserId) return Forbidden("You can only delete categories of your own events.");

        if (await _db.Enrolments.AnyAsync(en => en.CategoryID == id))
            return Conflict(new ErrorResponse("This category has enrolments and cannot be deleted."));

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
