using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;
using XSched.API.DbContexts;
using XSched.API.Entities;
using XSched.API.Models;
using XSched.API.Orchestrators.Interfaces;

namespace XSched.API.Orchestrators.Implementations;

public class CalendarEventsOrchestrator : ICalendarEventsOrchestrator
{
    private readonly XSchedDbContext _dbContext;

    public CalendarEventsOrchestrator(XSchedDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public IQueryable<CalendarEvent> GetUserCalendarEvents(ApplicationUser user)
    {
        return _dbContext.CalendarEvents.Include(x => x.Profile).Where(e => e.Profile.UserId == user.Id);
    }

    public IQueryable<CalendarEvent> GetUserCalendarEvent(ApplicationUser user, Guid calendarEventId)
    {
        return _dbContext.CalendarEvents.Include(x => x.Profile)
            .Where(e => e.Profile.UserId == user.Id && e.Id == calendarEventId);
    }

    public Task<CalendarEvent?> GetUserCalendarEventAsync(ApplicationUser user, Guid calendarEventId)
    {
        return _dbContext.CalendarEvents.Include(x => x.Profile)
            .FirstOrDefaultAsync(e => e.Profile.UserId == user.Id && e.Id == calendarEventId);
    }

    public async Task<CalendarEvent> CreateCalendarEventAsync(ApplicationUser user, CalendarEvent calendarEvent)
    {
        // TODO: assign default calendar profile
        _dbContext.CalendarEvents.Add(calendarEvent);
        await _dbContext.SaveChangesAsync();
        return calendarEvent;
    }

    public Task<CalendarEvent> CreateCalendarEventAsync(ApplicationUser user, CalendarEvent calendarEvent,
        Guid calendarEventId)
    {
        calendarEvent.Id = calendarEventId;
        if (calendarEvent.AllDay.GetValueOrDefault()) calendarEvent.EndDate = calendarEvent.StartDate;
        return CreateCalendarEventAsync(user, calendarEvent);
    }

    public async Task<CalendarEvent> UpdateCalendarEventAsync(ApplicationUser user, CalendarEvent calendarEvent,
        CalendarEvent calendarEventDb)
    {
        calendarEvent.Id = calendarEventDb.Id;
        if (calendarEvent.AllDay.GetValueOrDefault()) calendarEvent.EndDate = calendarEvent.StartDate;
        _dbContext.Entry(calendarEventDb).CurrentValues.SetValues(calendarEvent);
        await _dbContext.SaveChangesAsync();

        return calendarEventDb;
    }

    public async Task<CalendarEvent> PartiallyUpdateCalendarEventAsync(ApplicationUser user, Delta<CalendarEvent> patch,
        CalendarEvent calendarEventDb)
    {
        patch.Patch(calendarEventDb);
        await _dbContext.SaveChangesAsync();

        return calendarEventDb;
    }

    public async Task DeleteCalendarEventAsync(ApplicationUser user, Guid calendarEventId)
    {
        var calendarEvent = await GetUserCalendarEventAsync(user, calendarEventId);
        if (calendarEvent == null)
            throw new FrontendException("Requested calendar event was not found", StatusCodes.Status404NotFound);

        _dbContext.CalendarEvents.Remove(calendarEvent);
        await _dbContext.SaveChangesAsync();
    }
}