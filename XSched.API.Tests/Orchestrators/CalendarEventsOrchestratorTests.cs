using EntityFrameworkCoreMock;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;
using Moq;
using XSched.API.DbContexts;
using XSched.API.Entities;
using XSched.API.Models;
using XSched.API.Orchestrators.Implementations;
using XSched.API.Tests.Helpers;

namespace XSched.API.Tests.Orchestrators;

public class CalendarEventsOrchestratorTests
{
    private Random _random;
    private Mock<XSchedDbContext> _dbContextMock;
    private CalendarEventsOrchestrator _eventsOrchestrator;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
        _dbContextMock = GetDbContextMock();
        _eventsOrchestrator = GetCalendarEventsOrchestrator();
    }

    [Test]
    public void GetUserCalendarEventsTest()
    {
        var user = _dbContextMock.Object.Users.FirstOrDefault() as ApplicationUser;

        var events = _eventsOrchestrator.GetUserCalendarEvents(user!).ToList();

        Assert.That(events.Count(), Is.EqualTo(1));
    }

    [Test]
    public void GetEmptyUserCalendarEventsTest()
    {
        var user = new ApplicationUser();

        var events = _eventsOrchestrator.GetUserCalendarEvents(user!).ToList();

        Assert.IsEmpty(events);
    }

    [Test]
    public void GetUserCalendarEventTest()
    {
        var targetEvent = _dbContextMock.Object.CalendarEvents.FirstOrDefault()!;
        var user = targetEvent.Profile.User;

        var events = _eventsOrchestrator.GetUserCalendarEvent(user!, targetEvent.Id).ToList();

        Assert.That(events.Count(), Is.EqualTo(1));
        Assert.That(events.FirstOrDefault(), Is.EqualTo(targetEvent));
    }

    [Test]
    public void GetUserCalendarEventNotFoundTest()
    {
        var targetEvent = _dbContextMock.Object.CalendarEvents.FirstOrDefault()!;
        var user = targetEvent.Profile.User;
        var unknownUser = new ApplicationUser();

        var parameters = new List<(ApplicationUser, Guid)>()
        {
            (user, Guid.Empty),
            (unknownUser, targetEvent.Id),
            (unknownUser, Guid.Empty)
        };

        foreach (var parameter in parameters)
        {
            var events = _eventsOrchestrator.GetUserCalendarEvent(parameter.Item1, parameter.Item2).ToList();

            Assert.IsEmpty(events);
        }
    }

    [Test]
    public async Task GetUserCalendarEventAsyncTest()
    {
        var targetEvent = _dbContextMock.Object.CalendarEvents.FirstOrDefault()!;
        var user = targetEvent.Profile.User;

        var calendarEvent = await _eventsOrchestrator.GetUserCalendarEventAsync(user!, targetEvent.Id);

        Assert.NotNull(calendarEvent);
        Assert.That(calendarEvent, Is.EqualTo(targetEvent));
    }

    [Test]
    public async Task GetUserCalendarEventAsyncNotFoundTest()
    {
        var targetEvent = _dbContextMock.Object.CalendarEvents.FirstOrDefault()!;
        var user = targetEvent.Profile.User;
        var unknownUser = new ApplicationUser();

        var parameters = new List<(ApplicationUser, Guid)>()
        {
            (user, Guid.Empty),
            (unknownUser, targetEvent.Id),
            (unknownUser, Guid.Empty)
        };

        foreach (var parameter in parameters)
        {
            var calendarEvent = await _eventsOrchestrator.GetUserCalendarEventAsync(parameter.Item1, parameter.Item2);

            Assert.Null(calendarEvent);
        }
    }

    [Test]
    public async Task CreateUserCalendarEventTest()
    {
        var profile = _dbContextMock.Object.Profiles.FirstOrDefault()!;
        var user = profile.User;

        var eventsDbSet = _dbContextMock.Object.CalendarEvents;
        var eventCount = eventsDbSet.Count();

        var calendarEvent = new CalendarEvent()
        {
            Id = Guid.NewGuid(),
            Title = GetRandomString(false),
            Description = GetRandomString(),
            StartDate = new DateTime(2022, 07, 01),
            EndDate = new DateTime(2022, 07, 02),
            AllDay = false,
            RecurrenceRule = GetRandomString(),
            RecurrenceException = GetRandomString(),
            ProfileId = profile.Id,
            Profile = profile
        };
        var calendarEventCopy = calendarEvent.Clone();

        calendarEvent = await _eventsOrchestrator.CreateCalendarEventAsync(user, calendarEvent);

        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount + 1));
        Assert.That(calendarEvent.Id, Is.EqualTo(calendarEventCopy.Id));
        Assert.That(calendarEvent.Title, Is.EqualTo(calendarEventCopy.Title));
        Assert.That(calendarEvent.Description, Is.EqualTo(calendarEventCopy.Description));
        Assert.That(calendarEvent.StartDate, Is.EqualTo(calendarEventCopy.StartDate));
        Assert.That(calendarEvent.EndDate, Is.EqualTo(calendarEventCopy.EndDate));
        Assert.That(calendarEvent.AllDay, Is.EqualTo(calendarEventCopy.AllDay));
        Assert.That(calendarEvent.RecurrenceRule, Is.EqualTo(calendarEventCopy.RecurrenceRule));
        Assert.That(calendarEvent.RecurrenceException, Is.EqualTo(calendarEventCopy.RecurrenceException));
        Assert.That(calendarEvent.ProfileId, Is.EqualTo(calendarEventCopy.ProfileId));
    }

    [Test]
    public async Task CreateUserCalendarEventWithIdTest()
    {
        var profile = _dbContextMock.Object.Profiles.FirstOrDefault()!;
        var user = profile.User;

        var eventsDbSet = _dbContextMock.Object.CalendarEvents;
        var eventCount = eventsDbSet.Count();

        var calendarEventId = Guid.NewGuid();
        var calendarEvent = new CalendarEvent()
        {
            Title = GetRandomString(false),
            Description = GetRandomString(),
            StartDate = new DateTime(2022, 07, 01),
            EndDate = new DateTime(2022, 07, 02),
            AllDay = false,
            RecurrenceRule = GetRandomString(),
            RecurrenceException = GetRandomString(),
            ProfileId = profile.Id,
            Profile = profile
        };
        var calendarEventCopy = calendarEvent.Clone();

        calendarEvent = await _eventsOrchestrator.CreateCalendarEventAsync(user, calendarEvent, calendarEventId);


        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount + 1));

        Assert.That(calendarEvent.Id, Is.EqualTo(calendarEventId));
        Assert.That(calendarEvent.Title, Is.EqualTo(calendarEventCopy.Title));
        Assert.That(calendarEvent.Description, Is.EqualTo(calendarEventCopy.Description));
        Assert.That(calendarEvent.StartDate, Is.EqualTo(calendarEventCopy.StartDate));
        Assert.That(calendarEvent.EndDate, Is.EqualTo(calendarEventCopy.EndDate));
        Assert.That(calendarEvent.AllDay, Is.EqualTo(calendarEventCopy.AllDay));
        Assert.That(calendarEvent.RecurrenceRule, Is.EqualTo(calendarEventCopy.RecurrenceRule));
        Assert.That(calendarEvent.RecurrenceException, Is.EqualTo(calendarEventCopy.RecurrenceException));
        Assert.That(calendarEvent.ProfileId, Is.EqualTo(calendarEventCopy.ProfileId));
    }

    [Test]
    public async Task UpdateUserCalendarEventTest()
    {
        var firstProfile = _dbContextMock.Object.Profiles.FirstOrDefault()!;
        var firstUser = firstProfile.User;

        var secondProfile = _dbContextMock.Object.Profiles.ToList()[1]!;
        var secondUser = secondProfile.User;

        var eventsDbSet = _dbContextMock.Object.CalendarEvents;
        var eventCount = eventsDbSet.Count();

        var calendarEvent = new CalendarEvent()
        {
            Id = Guid.NewGuid(),
            Title = GetRandomString(false),
            Description = GetRandomString(),
            StartDate = new DateTime(2022, 07, 01),
            EndDate = new DateTime(2022, 07, 02),
            AllDay = false,
            RecurrenceRule = GetRandomString(),
            RecurrenceException = GetRandomString(),
            ProfileId = firstProfile.Id,
            Profile = firstProfile
        };
        var calendarEventCopy = calendarEvent.Clone();

        calendarEvent = await _eventsOrchestrator.CreateCalendarEventAsync(firstUser, calendarEvent);

        var calendarEventUpdate = new CalendarEvent()
        {
            Id = calendarEvent.Id,
            Title = GetRandomString(false),
            Description = GetRandomString(),
            StartDate = new DateTime(2022, 07, 03),
            EndDate = new DateTime(2022, 07, 04),
            AllDay = false,
            RecurrenceRule = GetRandomString(),
            RecurrenceException = GetRandomString(),
            ProfileId = secondProfile.Id,
            Profile = secondProfile
        };
        var calendarEventUpdateCopy = calendarEventUpdate.Clone();
        calendarEventUpdate =
            await _eventsOrchestrator.UpdateCalendarEventAsync(secondUser, calendarEventUpdate, calendarEvent);


        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount + 1));

        Assert.That(calendarEventUpdate.Id, Is.EqualTo(calendarEventUpdateCopy.Id));
        Assert.That(calendarEventUpdate.Title, Is.EqualTo(calendarEventUpdateCopy.Title));
        Assert.That(calendarEventUpdate.Description, Is.EqualTo(calendarEventUpdateCopy.Description));
        Assert.That(calendarEventUpdate.StartDate, Is.EqualTo(calendarEventUpdateCopy.StartDate));
        Assert.That(calendarEventUpdate.EndDate, Is.EqualTo(calendarEventUpdateCopy.EndDate));
        Assert.That(calendarEventUpdate.AllDay, Is.EqualTo(calendarEventUpdateCopy.AllDay));
        Assert.That(calendarEventUpdate.RecurrenceRule, Is.EqualTo(calendarEventUpdateCopy.RecurrenceRule));
        Assert.That(calendarEventUpdate.RecurrenceException, Is.EqualTo(calendarEventUpdateCopy.RecurrenceException));
        Assert.That(calendarEventUpdate.ProfileId, Is.EqualTo(calendarEventUpdateCopy.ProfileId));

        Assert.That(calendarEventUpdate.Id, Is.EqualTo(calendarEventCopy.Id));
        Assert.That(calendarEventUpdate.Title, Is.Not.EqualTo(calendarEventCopy.Title));
        Assert.That(calendarEventUpdate.Description, Is.Not.EqualTo(calendarEventCopy.Description));
        Assert.That(calendarEventUpdate.StartDate, Is.Not.EqualTo(calendarEventCopy.StartDate));
        Assert.That(calendarEventUpdate.EndDate, Is.Not.EqualTo(calendarEventCopy.EndDate));
        Assert.That(calendarEventUpdate.AllDay, Is.EqualTo(calendarEventCopy.AllDay));
        Assert.That(calendarEventUpdate.RecurrenceRule, Is.Not.EqualTo(calendarEventCopy.RecurrenceRule));
        Assert.That(calendarEventUpdate.RecurrenceException, Is.Not.EqualTo(calendarEventCopy.RecurrenceException));
        Assert.That(calendarEventUpdate.ProfileId, Is.Not.EqualTo(calendarEventCopy.ProfileId));
    }

    [Test]
    public async Task PartiallyUpdateUserCalendarEventTest()
    {
        var firstProfile = _dbContextMock.Object.Profiles.FirstOrDefault()!;
        var firstUser = firstProfile.User;

        var secondProfile = _dbContextMock.Object.Profiles.ToList()[1]!;
        var secondUser = secondProfile.User;

        var eventsDbSet = _dbContextMock.Object.CalendarEvents;
        var eventCount = eventsDbSet.Count();

        var calendarEvent = new CalendarEvent()
        {
            Id = Guid.NewGuid(),
            Title = GetRandomString(false),
            Description = GetRandomString(),
            StartDate = new DateTime(2022, 07, 01),
            EndDate = new DateTime(2022, 07, 02),
            AllDay = false,
            RecurrenceRule = GetRandomString(),
            RecurrenceException = GetRandomString(),
            ProfileId = firstProfile.Id,
            Profile = firstProfile
        };
        var calendarEventCopy = calendarEvent.Clone();

        calendarEvent = await _eventsOrchestrator.CreateCalendarEventAsync(firstUser, calendarEvent);

        var patch = new Delta<CalendarEvent>();
        patch.TrySetPropertyValue("Id", calendarEvent.Id);
        patch.TrySetPropertyValue("Title", GetRandomString(false));
        patch.TrySetPropertyValue("Description", GetRandomString());
        patch.TrySetPropertyValue("StartDate", new DateTime(2022, 07, 03));
        patch.TrySetPropertyValue("EndDate", new DateTime(2022, 07, 04));
        patch.TrySetPropertyValue("RecurrenceRule", GetRandomString());
        patch.TrySetPropertyValue("RecurrenceException", GetRandomString());
        patch.TrySetPropertyValue("ProfileId", secondProfile.Id);
        patch.TrySetPropertyValue("Profile", secondProfile);

        var calendarEventUpdateCopy = patch.GetInstance().Clone();
        var calendarEventUpdate =
            await _eventsOrchestrator.PartiallyUpdateCalendarEventAsync(secondUser, patch, calendarEvent);


        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount + 1));

        Assert.That(calendarEventUpdate.Id, Is.EqualTo(calendarEventUpdateCopy.Id));
        Assert.That(calendarEventUpdate.Title, Is.EqualTo(calendarEventUpdateCopy.Title));
        Assert.That(calendarEventUpdate.Description, Is.EqualTo(calendarEventUpdateCopy.Description));
        Assert.That(calendarEventUpdate.StartDate, Is.EqualTo(calendarEventUpdateCopy.StartDate));
        Assert.That(calendarEventUpdate.EndDate, Is.EqualTo(calendarEventUpdateCopy.EndDate));
        // Assert.That(calendarEventUpdate.AllDay, Is.EqualTo(calendarEventUpdateCopy.AllDay));
        Assert.That(calendarEventUpdate.RecurrenceRule, Is.EqualTo(calendarEventUpdateCopy.RecurrenceRule));
        Assert.That(calendarEventUpdate.RecurrenceException, Is.EqualTo(calendarEventUpdateCopy.RecurrenceException));
        Assert.That(calendarEventUpdate.ProfileId, Is.EqualTo(calendarEventUpdateCopy.ProfileId));

        Assert.That(calendarEventUpdate.Id, Is.EqualTo(calendarEventCopy.Id));
        Assert.That(calendarEventUpdate.Title, Is.Not.EqualTo(calendarEventCopy.Title));
        Assert.That(calendarEventUpdate.Description, Is.Not.EqualTo(calendarEventCopy.Description));
        Assert.That(calendarEventUpdate.StartDate, Is.Not.EqualTo(calendarEventCopy.StartDate));
        Assert.That(calendarEventUpdate.EndDate, Is.Not.EqualTo(calendarEventCopy.EndDate));
        Assert.That(calendarEventUpdate.AllDay, Is.EqualTo(calendarEventCopy.AllDay));
        Assert.That(calendarEventUpdate.RecurrenceRule, Is.Not.EqualTo(calendarEventCopy.RecurrenceRule));
        Assert.That(calendarEventUpdate.RecurrenceException, Is.Not.EqualTo(calendarEventCopy.RecurrenceException));
        Assert.That(calendarEventUpdate.ProfileId, Is.Not.EqualTo(calendarEventCopy.ProfileId));
    }

    [Test]
    public async Task DeleteCalendarUserEvent()
    {
        var eventsDbSet = _dbContextMock.Object.CalendarEvents;
        var eventCount = eventsDbSet.Count();
        var targetEvent = eventsDbSet.FirstOrDefault()!;

        var profile = targetEvent.Profile;
        var user = profile.User;

        await _eventsOrchestrator.DeleteCalendarEventAsync(user, targetEvent.Id);

        var foundEvent = eventsDbSet.FirstOrDefault(e => e.Id == targetEvent.Id);
        Assert.Null(foundEvent);
        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount - 1));
    }

    [Test]
    public void DeleteCalendarUserErrorsEvent()
    {
        var eventsDbSet = _dbContextMock.Object.CalendarEvents;
        var eventCount = eventsDbSet.Count();
        var targetEvent = eventsDbSet.FirstOrDefault()!;

        var profile = targetEvent.Profile;
        var user = profile.User;
        var unknownUser = new ApplicationUser();

        var parameters = new List<(ApplicationUser, Guid)>()
        {
            (unknownUser, targetEvent.Id),
            (user!, Guid.NewGuid()),
            (unknownUser, Guid.NewGuid())
        };

        var actualMessage = "Requested calendar event was not found";
        foreach (var parameter in parameters)
        {
            var throws = Assert.ThrowsAsync<FrontendException>(
                async () => { await _eventsOrchestrator.DeleteCalendarEventAsync(parameter.Item1, parameter.Item2); },
                $"Expected the following exception message: {actualMessage}");
            Assert.That(throws!.Messages.Count(), Is.EqualTo(1));
            Assert.That(throws.Message, Is.EqualTo(actualMessage));
            Assert.That(throws.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount));
    }

    private Mock<XSchedDbContext> GetDbContextMock()
    {
        var optionsBuilder = new DbContextOptionsBuilder<XSchedDbContext>();
        optionsBuilder.UseInMemoryDatabase("TestDatabase");

        var dbContextMock = new DbContextMock<XSchedDbContext>(optionsBuilder.Options) { CallBase = true };

        SetupUsersDbSetMock(dbContextMock);
        SetupUserProfilesDbSetMock(dbContextMock);
        SetupUserCalendarEventsDbSetMock(dbContextMock);

        return dbContextMock;
    }

    private void SetupUsersDbSetMock(DbContextMock<XSchedDbContext> dbContextMock)
    {
        var users = new List<ApplicationUser>()
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                UserName = _random.Next(100000, 999999).ToString()
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                UserName = _random.Next(100000, 999999).ToString()
            }
        };
        dbContextMock.CreateDbSetMock(x => x.Users, users);
    }

    private void SetupUserProfilesDbSetMock(DbContextMock<XSchedDbContext> dbContextMock)
    {
        var usersDbSet = dbContextMock.Object.Users;
        var firstUser = usersDbSet.FirstOrDefault() as ApplicationUser;
        var secondUser = usersDbSet.ToList()[1] as ApplicationUser;

        var userProfiles = new List<UserProfile>()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = _random.Next(100000, 999999).ToString(),
                User = firstUser,
                UserId = firstUser!.Id
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = _random.Next(100000, 999999).ToString(),
                User = secondUser,
                UserId = secondUser!.Id
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = _random.Next(100000, 999999).ToString(),
                User = secondUser,
                UserId = secondUser!.Id
            }
        };
        dbContextMock.CreateDbSetMock(x => x.Profiles, userProfiles);
    }

    private void SetupUserCalendarEventsDbSetMock(DbContextMock<XSchedDbContext> dbContextMock)
    {
        var profilesDbSet = dbContextMock.Object.Profiles;
        var firstProfile = profilesDbSet.FirstOrDefault()!;
        var secondProfile = profilesDbSet.ToList()[1]!;

        var events = new List<CalendarEvent>()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = GetRandomString(),
                Description = GetRandomString(true),
                StartDate = new DateTime(2022, 07, 01),
                EndDate = new DateTime(2022, 07, 02),
                AllDay = false,
                ProfileId = firstProfile.Id,
                Profile = firstProfile
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = GetRandomString(),
                Description = GetRandomString(true),
                StartDate = new DateTime(2022, 07, 01),
                EndDate = new DateTime(2022, 07, 02),
                AllDay = false,
                ProfileId = secondProfile.Id,
                Profile = secondProfile
            }
        };
        dbContextMock.CreateDbSetMock(x => x.CalendarEvents, events);
    }

    private CalendarEventsOrchestrator GetCalendarEventsOrchestrator()
    {
        return new CalendarEventsOrchestrator(_dbContextMock.Object);
    }

    private string GetRandomString(bool longString = false)
    {
        var start = longString ? 100000 : 1000;
        var end = longString ? 999999 : 9999;
        return _random.Next(start, end).ToString();
    }
}