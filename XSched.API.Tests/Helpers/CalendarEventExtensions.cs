using XSched.API.Entities;

namespace XSched.API.Tests.Helpers;

public static class CalendarEventExtensions
{
    public static CalendarEvent Clone(this CalendarEvent calendarEvent)
    {
        return new CalendarEvent()
        {
            Id = calendarEvent.Id,
            Title = calendarEvent.Title,
            Description = calendarEvent.Description,
            StartDate = calendarEvent.StartDate,
            EndDate = calendarEvent.EndDate,
            AllDay = calendarEvent.AllDay,
            RecurrenceRule = calendarEvent.RecurrenceRule,
            RecurrenceException = calendarEvent.RecurrenceException,
            ProfileId = calendarEvent.ProfileId,
            Profile = calendarEvent.Profile
        };
    }
}