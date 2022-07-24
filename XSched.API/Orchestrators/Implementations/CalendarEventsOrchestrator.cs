using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;
using XSched.API.DbContexts;
using XSched.API.Entities;
using XSched.API.Models;
using XSched.API.Orchestrators.Interfaces;
using XSched.API.Repositories.Interfaces;

namespace XSched.API.Orchestrators.Implementations;

public class CalendarEventsOrchestrator : ICalendarEventsOrchestrator
{
    private readonly XSchedDbContext _dbContext;
    private readonly IProfileRepository _profileRepository;

    public CalendarEventsOrchestrator(XSchedDbContext dbContext, IProfileRepository profileRepository)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _profileRepository = profileRepository;
    }

    public Task<CalendarEvent?> GetCalendarEventAsync(Guid calendarEventId)
    {
        return _dbContext.CalendarEvents.Include(x => x.Profile).FirstOrDefaultAsync(e => e.Id == calendarEventId);
    }

    public virtual IQueryable<CalendarEvent> GetUserCalendarEvents(ApplicationUser user)
    {
        return _dbContext.CalendarEvents.Include(x => x.Profile).Where(e => e.Profile.UserId == user.Id);
    }

    public virtual IQueryable<CalendarEvent> GetUserCalendarEvent(ApplicationUser user, Guid calendarEventId)
    {
        return _dbContext.CalendarEvents.Include(x => x.Profile)
            .Where(e => e.Profile.UserId == user.Id && e.Id == calendarEventId);
    }

    public Task<CalendarEvent?> GetUserCalendarEventAsync(ApplicationUser user, Guid calendarEventId)
    {
        return _dbContext.CalendarEvents.Include(x => x.Profile)
            .FirstOrDefaultAsync(e => e.Profile.UserId == user.Id && e.Id == calendarEventId);
    }

    public virtual async Task<CalendarEvent> CreateCalendarEventAsync(ApplicationUser user, CalendarEvent calendarEvent)
    {
        if (calendarEvent.AllDay.GetValueOrDefault()) calendarEvent.EndDate = calendarEvent.StartDate;

        if (calendarEvent.ProfileId == Guid.Empty) await AssignDefaultProfile(user.Id, calendarEvent);

        _dbContext.CalendarEvents.Add(calendarEvent);
        await _dbContext.SaveChangesAsync();

        return calendarEvent;
    }

    public virtual Task<CalendarEvent> CreateCalendarEventAsync(ApplicationUser user, CalendarEvent calendarEvent,
        Guid calendarEventId)
    {
        calendarEvent.Id = calendarEventId;

        return CreateCalendarEventAsync(user, calendarEvent);
    }

    public virtual async Task<CalendarEvent> UpdateCalendarEventAsync(ApplicationUser user, CalendarEvent calendarEvent,
        CalendarEvent calendarEventDb)
    {
        calendarEvent.Id = calendarEventDb.Id;

        if (calendarEvent.AllDay.GetValueOrDefault()) calendarEvent.EndDate = calendarEvent.StartDate;

        _dbContext.Entry(calendarEventDb).CurrentValues.SetValues(calendarEvent);
        await _dbContext.SaveChangesAsync();

        return calendarEventDb;
    }

    public virtual async Task<CalendarEvent> PartiallyUpdateCalendarEventAsync(ApplicationUser user,
        Delta<CalendarEvent> patch,
        CalendarEvent calendarEventDb)
    {
        patch.TrySetPropertyValue("Id", calendarEventDb.Id);

        patch.Patch(calendarEventDb);
        await _dbContext.SaveChangesAsync();

        return calendarEventDb;
    }

    public virtual async Task DeleteCalendarEventAsync(ApplicationUser user, Guid calendarEventId)
    {
        var calendarEvent = await GetUserCalendarEventAsync(user, calendarEventId);
        if (calendarEvent == null)
            throw new FrontendException("Requested calendar event was not found", StatusCodes.Status404NotFound);

        _dbContext.CalendarEvents.Remove(calendarEvent);
        await _dbContext.SaveChangesAsync();
    }

    private async Task AssignDefaultProfile(string userId, CalendarEvent calendarEvent)
    {
        var profile = await _profileRepository.GetDefaultUserProfileAsync(userId);
        if (profile == null)
            throw new FrontendException(
                "Cannot find default profile for the event. Please specify ProfileId field.");

        calendarEvent.ProfileId = profile.Id;
        calendarEvent.Profile = profile;
    }
}