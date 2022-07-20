using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using XSched.API.Entities;
using XSched.API.Orchestrators.Interfaces;

namespace XSched.API.Controllers;

[Authorize]
[Route("odata/calendarEvents")]
public class CalendarEventsController : ODataController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICalendarEventsOrchestrator _eventsOrchestrator;

    public CalendarEventsController(UserManager<ApplicationUser> userManager,
        ICalendarEventsOrchestrator eventsOrchestrator)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _eventsOrchestrator = eventsOrchestrator ?? throw new ArgumentNullException(nameof(eventsOrchestrator));
    }

    [HttpGet]
    [EnableQuery]
    public async Task<IActionResult> GetUserCalendarEvents()
    {
        var user = await GetCurrentUser();

        return Ok(_eventsOrchestrator.GetUserCalendarEvents(user));
    }

    [HttpGet("({eventId})")]
    [EnableQuery]
    public async Task<IActionResult> GetUserCalendarEvent(Guid eventId)
    {
        var user = await GetCurrentUser();

        var result = _eventsOrchestrator.GetUserCalendarEvent(user, eventId);
        return Ok(SingleResult.Create(result));
    }

    [HttpPost]
    [EnableQuery]
    public async Task<IActionResult> CreateUserCalendarEvent([FromBody] CalendarEvent calendarEvent)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUser();

        var result = await _eventsOrchestrator.CreateCalendarEventAsync(user, calendarEvent);
        return Created(result);
    }

    [HttpPut("({eventId})")]
    [EnableQuery]
    public async Task<IActionResult> UpdateUserCalendarEvent(Guid eventId, [FromBody] CalendarEvent calendarEvent)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUser();

        var calendarEventDb = await _eventsOrchestrator.GetCalendarEventAsync(eventId);
        if (calendarEventDb == null)
        {
            var createResult = await _eventsOrchestrator.CreateCalendarEventAsync(user, calendarEvent, eventId);
            return Created(createResult);
        }

        if (calendarEventDb.Profile.UserId != user.Id) return Forbid();


        var updateResult = await _eventsOrchestrator.UpdateCalendarEventAsync(user, calendarEvent, calendarEventDb);
        return Ok(updateResult);
    }

    [HttpPatch("({eventId})")]
    [EnableQuery]
    public async Task<IActionResult> PartiallyUpdateUserCalendarEvent(Guid eventId,
        [FromBody] Delta<CalendarEvent> patch)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUser();

        var calendarEventDb = await _eventsOrchestrator.GetCalendarEventAsync(eventId);
        if (calendarEventDb == null)
        {
            var calendarEvent = patch.GetInstance();
            var createResult = await _eventsOrchestrator.CreateCalendarEventAsync(user, calendarEvent, eventId);
            return Created(createResult);
        }

        if (calendarEventDb.Profile.UserId != user.Id) return Forbid();

        var updateResult = await _eventsOrchestrator.PartiallyUpdateCalendarEventAsync(user, patch, calendarEventDb);
        return Ok(updateResult);
    }

    [HttpDelete("({eventId})")]
    public async Task<IActionResult> DeleteUserCalendarEvent(Guid eventId)
    {
        var user = await GetCurrentUser();

        await _eventsOrchestrator.DeleteCalendarEventAsync(user, eventId);
        return NoContent();
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    public virtual async Task<ApplicationUser> GetCurrentUser()
    {
        var username = HttpContext.User.Identity!.Name;
        var user = await _userManager.FindByNameAsync(username);
        return user;
    }
}