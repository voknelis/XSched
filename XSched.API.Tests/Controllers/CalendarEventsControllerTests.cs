using EntityFrameworkCoreMock;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.EntityFrameworkCore;
using Moq;
using XSched.API.Controllers;
using XSched.API.DbContexts;
using XSched.API.Entities;
using XSched.API.Orchestrators.Implementations;
using XSched.API.Repositories.Implementation;

namespace XSched.API.Tests.Controllers;

public class CalendarEventsControllerTests
{
    private Random _random;
    private Mock<XSchedDbContext> _dbContextMock;
    private ProfileRepository _profileRepository;
    private Mock<CalendarEventsOrchestrator> _eventsOrchestratorMock;
    private Mock<UserManager<ApplicationUser>> _userManagerMock;
    private Mock<CalendarEventsController> _eventsControllerMock;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
        _dbContextMock = GetDbContextMock();
        _profileRepository = new ProfileRepository(_dbContextMock.Object);
        _eventsOrchestratorMock = GetCalendarEventsOrchestratorMock();
        _userManagerMock = GetUserManagerMock();
        _eventsControllerMock = GetCalendarEventsControllerMock();
    }

    [Test]
    public async Task GetUserCalendarEventsTest()
    {
        var dbContextMock = _dbContextMock.Object;
        var user = dbContextMock.Users.First() as ApplicationUser;
        _eventsControllerMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user!);

        var targetEvent = dbContextMock.CalendarEvents.First();


        var result = await _eventsControllerMock.Object.GetUserCalendarEvents() as OkObjectResult;

        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        var eventsQueryable = result.Value as IQueryable<CalendarEvent>;
        var events = eventsQueryable!.ToList();
        Assert.NotNull(events);
        Assert.That(events!.Count, Is.EqualTo(1));
        Assert.That(events.First(), Is.EqualTo(targetEvent));

        _eventsOrchestratorMock.Verify(x => x.GetUserCalendarEvents(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Test]
    public async Task GetUserCalendarEventTest()
    {
        var dbContextMock = _dbContextMock.Object;
        var user = dbContextMock.Users.First() as ApplicationUser;
        _eventsControllerMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user!);

        var targetEvent = dbContextMock.CalendarEvents.First();


        var result = await _eventsControllerMock.Object.GetUserCalendarEvent(targetEvent.Id) as OkObjectResult;

        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        var eventsQueryable = result.Value as SingleResult<CalendarEvent>;
        var calendarEvent = eventsQueryable!.Queryable.Single();
        Assert.NotNull(calendarEvent);
        Assert.That(calendarEvent, Is.EqualTo(targetEvent));

        _eventsOrchestratorMock.Verify(x => x.GetUserCalendarEvent(It.IsAny<ApplicationUser>(), It.IsAny<Guid>()),
            Times.Once);
    }

    [Test]
    public async Task CreateUserCalendarEventTest()
    {
        var dbContextMock = _dbContextMock.Object;
        var user = dbContextMock.Users.First() as ApplicationUser;
        _eventsControllerMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user!);

        var profile = dbContextMock.Profiles.First();

        var eventsDbSet = _dbContextMock.Object.CalendarEvents;
        var eventCount = eventsDbSet.Count();

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


        var result =
            await _eventsControllerMock.Object.CreateUserCalendarEvent(calendarEvent) as
                CreatedODataResult<CalendarEvent>;

        Assert.NotNull(result);
        Assert.NotNull(result!.Entity);
        Assert.That(result.Entity, Is.EqualTo(calendarEvent));

        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount + 1));
        _eventsOrchestratorMock.Verify(
            x => x.CreateCalendarEventAsync(It.IsAny<ApplicationUser>(), It.IsAny<CalendarEvent>()),
            Times.Once);
    }

    [Test]
    public async Task CreateUserCalendarEventModelErrorTest()
    {
        _eventsControllerMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(() => null!);
        _eventsControllerMock.Object.ModelState.AddModelError("calendarEvent", "The calendarEvent field is required.");


        var result = await _eventsControllerMock.Object.CreateUserCalendarEvent(null!) as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

        _eventsOrchestratorMock.Verify(
            x => x.CreateCalendarEventAsync(It.IsAny<ApplicationUser>(), It.IsAny<CalendarEvent>()),
            Times.Never);
    }

    [Test]
    public async Task CreateUserCalendarEventWithIdTest()
    {
        var dbContextMock = _dbContextMock.Object;
        var user = dbContextMock.Users.First() as ApplicationUser;
        _eventsControllerMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user!);

        var profile = dbContextMock.Profiles.First();

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


        var result =
            await _eventsControllerMock.Object.UpdateUserCalendarEvent(calendarEvent.Id, calendarEvent) as
                CreatedODataResult<CalendarEvent>;

        Assert.NotNull(result);
        Assert.NotNull(result!.Entity);
        Assert.That(result.Entity, Is.EqualTo(calendarEvent));

        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount + 1));
        _eventsOrchestratorMock.Verify(
            x => x.CreateCalendarEventAsync(It.IsAny<ApplicationUser>(), It.IsAny<CalendarEvent>()), Times.Once);
    }

    [Test]
    public async Task UpdateUserCalendarEventTest()
    {
        var dbContextMock = _dbContextMock.Object;
        var user = dbContextMock.Users.First() as ApplicationUser;
        _eventsControllerMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user!);

        var profile = dbContextMock.Profiles.First();

        var eventsDbSet = _dbContextMock.Object.CalendarEvents;
        var eventCount = eventsDbSet.Count();

        var calendarEvent = new CalendarEvent()
        {
            Title = GetRandomString(false),
            Description = GetRandomString(),
            StartDate = new DateTime(2022, 07, 03),
            EndDate = new DateTime(2022, 07, 04),
            AllDay = false,
            RecurrenceRule = GetRandomString(),
            RecurrenceException = GetRandomString(),
            ProfileId = profile.Id,
            Profile = profile
        };

        var targetEvent = dbContextMock.CalendarEvents.First();


        var result =
            await _eventsControllerMock.Object.UpdateUserCalendarEvent(targetEvent.Id, calendarEvent) as
                OkObjectResult;


        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.NotNull(result.Value);

        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount));
        _eventsOrchestratorMock.Verify(
            x => x.UpdateCalendarEventAsync(It.IsAny<ApplicationUser>(), It.IsAny<CalendarEvent>(),
                It.IsAny<CalendarEvent>()), Times.Once);
    }

    [Test]
    public async Task UpdateUserCalendarEventWithoutUserAccessTest()
    {
        var dbContextMock = _dbContextMock.Object;
        var user = dbContextMock.Users.Last() as ApplicationUser;
        _eventsControllerMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user!);

        var profile = dbContextMock.Profiles.First();

        var eventsDbSet = _dbContextMock.Object.CalendarEvents;
        var eventCount = eventsDbSet.Count();

        var calendarEvent = new CalendarEvent()
        {
            Title = GetRandomString(false),
            Description = GetRandomString(),
            StartDate = new DateTime(2022, 07, 03),
            EndDate = new DateTime(2022, 07, 04),
            AllDay = false,
            RecurrenceRule = GetRandomString(),
            RecurrenceException = GetRandomString(),
            ProfileId = profile.Id,
            Profile = profile
        };

        var targetEvent = dbContextMock.CalendarEvents.First();


        var result =
            await _eventsControllerMock.Object.UpdateUserCalendarEvent(targetEvent.Id, calendarEvent) as
                ForbidResult;

        Assert.NotNull(result);
        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount));

        _eventsOrchestratorMock.Verify(
            x => x.CreateCalendarEventAsync(It.IsAny<ApplicationUser>(), It.IsAny<CalendarEvent>()), Times.Never);
        _eventsOrchestratorMock.Verify(
            x => x.UpdateCalendarEventAsync(It.IsAny<ApplicationUser>(), It.IsAny<CalendarEvent>(),
                It.IsAny<CalendarEvent>()), Times.Never);
    }

    [Test]
    public async Task PartiallyUpdateUserCalendarEventTest()
    {
        var dbContextMock = _dbContextMock.Object;
        var user = dbContextMock.Users.First() as ApplicationUser;
        _eventsControllerMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user!);

        var eventsDbSet = _dbContextMock.Object.CalendarEvents;
        var eventCount = eventsDbSet.Count();

        var targetEvent = dbContextMock.CalendarEvents.First();
        var patch = new Delta<CalendarEvent>();


        var result =
            await _eventsControllerMock.Object.PartiallyUpdateUserCalendarEvent(targetEvent.Id,
                patch) as OkObjectResult;

        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.NotNull(result.Value);

        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount));
        _eventsOrchestratorMock.Verify(
            x => x.PartiallyUpdateCalendarEventAsync(It.IsAny<ApplicationUser>(), It.IsAny<Delta<CalendarEvent>>(),
                It.IsAny<CalendarEvent>()), Times.Once);
    }

    [Test]
    public async Task PartiallyUpdateUserCalendarEventWithoutUserAccessTest()
    {
        var dbContextMock = _dbContextMock.Object;
        var user = dbContextMock.Users.Last() as ApplicationUser;
        _eventsControllerMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user!);

        var eventsDbSet = _dbContextMock.Object.CalendarEvents;
        var eventCount = eventsDbSet.Count();

        var targetEvent = dbContextMock.CalendarEvents.First();
        var patch = new Delta<CalendarEvent>();

        var result =
            await _eventsControllerMock.Object.PartiallyUpdateUserCalendarEvent(targetEvent.Id,
                patch) as ForbidResult;


        Assert.NotNull(result);

        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount));
        _eventsOrchestratorMock.Verify(
            x => x.PartiallyUpdateCalendarEventAsync(It.IsAny<ApplicationUser>(), It.IsAny<Delta<CalendarEvent>>(),
                It.IsAny<CalendarEvent>()), Times.Never);
    }

    [Test]
    public async Task DeleteUserCalendarEventTest()
    {
        var dbContextMock = _dbContextMock.Object;
        var targetEvent = dbContextMock.CalendarEvents.First();
        var user = targetEvent.Profile.User;
        _eventsControllerMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user);

        var eventsDbSet = _dbContextMock.Object.CalendarEvents;
        var eventCount = eventsDbSet.Count();


        var result = await _eventsControllerMock.Object.DeleteUserCalendarEvent(targetEvent.Id) as NoContentResult;

        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status204NoContent));

        Assert.That(eventsDbSet.Count(), Is.EqualTo(eventCount - 1));
        _eventsOrchestratorMock.Verify(x => x.DeleteCalendarEventAsync(It.IsAny<ApplicationUser>(), It.IsAny<Guid>()),
            Times.Once);
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
                UserName = GetRandomString()
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                UserName = GetRandomString()
            }
        };
        dbContextMock.CreateDbSetMock(x => x.Users, users);
    }

    private void SetupUserProfilesDbSetMock(DbContextMock<XSchedDbContext> dbContextMock)
    {
        var usersDbSet = dbContextMock.Object.Users;
        var firstUser = usersDbSet.First() as ApplicationUser;
        var secondUser = usersDbSet.Last() as ApplicationUser;

        var userProfiles = new List<UserProfile>()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = GetRandomString(),
                User = firstUser,
                UserId = firstUser!.Id,
                IsDefault = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = GetRandomString(),
                User = firstUser,
                UserId = firstUser!.Id
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = GetRandomString(),
                User = secondUser,
                UserId = secondUser!.Id,
                IsDefault = true
            }
        };
        dbContextMock.CreateDbSetMock(x => x.Profiles, userProfiles);
    }

    private void SetupUserCalendarEventsDbSetMock(DbContextMock<XSchedDbContext> dbContextMock)
    {
        var profilesDbSet = dbContextMock.Object.Profiles;
        var firstProfile = profilesDbSet.First();
        var secondProfile = profilesDbSet.Last();

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

    private Mock<CalendarEventsOrchestrator> GetCalendarEventsOrchestratorMock()
    {
        return new Mock<CalendarEventsOrchestrator>(_dbContextMock.Object, _profileRepository) { CallBase = true };
    }

    private Mock<UserManager<ApplicationUser>> GetUserManagerMock()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock =
            new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null,
                null);

        return userManagerMock;
    }

    private Mock<CalendarEventsController> GetCalendarEventsControllerMock()
    {
        return new Mock<CalendarEventsController>(_userManagerMock.Object, _eventsOrchestratorMock.Object)
            { CallBase = true };
    }

    private string GetRandomString(bool longString = false)
    {
        var start = longString ? 100000 : 1000;
        var end = longString ? 999999 : 9999;
        return _random.Next(start, end).ToString();
    }
}