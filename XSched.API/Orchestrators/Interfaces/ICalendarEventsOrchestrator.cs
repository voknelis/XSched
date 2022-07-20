using Microsoft.AspNetCore.OData.Deltas;
using XSched.API.Entities;

namespace XSched.API.Orchestrators.Interfaces;

public interface ICalendarEventsOrchestrator
{
    public Task<CalendarEvent?> GetCalendarEventAsync(Guid calendarEventId);
    public IQueryable<CalendarEvent> GetUserCalendarEvents(ApplicationUser user);
    public IQueryable<CalendarEvent> GetUserCalendarEvent(ApplicationUser user, Guid calendarEventId);
    public Task<CalendarEvent?> GetUserCalendarEventAsync(ApplicationUser user, Guid calendarEventId);
    public Task<CalendarEvent> CreateCalendarEventAsync(ApplicationUser user, CalendarEvent calendarEvent);

    public Task<CalendarEvent> CreateCalendarEventAsync(ApplicationUser user, CalendarEvent calendarEvent,
        Guid calendarEventId);

    public Task<CalendarEvent> UpdateCalendarEventAsync(ApplicationUser user, CalendarEvent calendarEvent,
        CalendarEvent calendarEventDb);

    public Task<CalendarEvent> PartiallyUpdateCalendarEventAsync(ApplicationUser user, Delta<CalendarEvent> patch,
        CalendarEvent calendarEventDb);

    public Task DeleteCalendarEventAsync(ApplicationUser user, Guid calendarEventId);
}