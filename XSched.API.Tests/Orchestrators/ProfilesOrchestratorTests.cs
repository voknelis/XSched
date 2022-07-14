using EntityFrameworkCoreMock;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;
using Moq;
using XSched.API.DbContexts;
using XSched.API.Entities;
using XSched.API.Models;
using XSched.API.Orchestrators.Implementations;
using XSched.API.Repositories.Implementation;
using XSched.API.Tests.Helpers;

namespace XSched.API.Tests.Orchestrators;

public class ProfilesOrchestratorTests
{
    private Random _random;
    private Mock<XSchedDbContext> _dbContextMock;
    private Mock<ProfileRepository> _profileRepositoryMock;
    private ProfilesOrchestrator _profileOrchestrator;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
        _dbContextMock = GetDbContextMock();
        _profileRepositoryMock = GetProfileRepositoryMock();
        _profileOrchestrator = GetProfileOrchestrator();
    }

    [Test]
    public async Task GetUserProfilesTest()
    {
        var usersDbSet = _dbContextMock.Object.Users;
        var firstUser = usersDbSet.FirstOrDefault() as ApplicationUser;
        var secondUser = usersDbSet.ToList()[1] as ApplicationUser;

        var profilesDbSet = _dbContextMock.Object.Profiles;
        Assert.That(profilesDbSet.Count(), Is.EqualTo(3));

        var profilesFirstUser = await _profileOrchestrator.GetUserProfiles(firstUser!);
        Assert.That(profilesFirstUser.Count(), Is.EqualTo(1));

        var profilesSecondUser = await _profileOrchestrator.GetUserProfiles(secondUser!);
        Assert.That(profilesSecondUser.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetEmptyUserProfilesTest()
    {
        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(3));

        var unknownUser = new ApplicationUser();

        var profilesUnknownUser = await _profileOrchestrator.GetUserProfiles(unknownUser);
        Assert.That(profilesUnknownUser.Count(), Is.EqualTo(0));
    }


    [Test]
    public async Task GetUserProfileTest()
    {
        var usersDbSet = _dbContextMock.Object.Users;
        var firstUser = usersDbSet.FirstOrDefault() as ApplicationUser;

        var profilesDbSet = _dbContextMock.Object.Profiles;

        var targetProfile = profilesDbSet.FirstOrDefault();
        var foundProfile = await _profileOrchestrator.GetUserProfile(firstUser!, targetProfile!.Id);
        Assert.That(foundProfile, Is.Not.Null);
        Assert.That(foundProfile.Id, Is.EqualTo(targetProfile.Id));
    }

    [Test]
    public void GetUserProfileErrorsTest()
    {
        var usersDbSet = _dbContextMock.Object.Users;
        var firstUser = usersDbSet.FirstOrDefault() as ApplicationUser;
        var unknownUser = new ApplicationUser();

        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();

        var targetProfile = profilesDbSet.FirstOrDefault();
        var arguments = new List<(ApplicationUser, Guid)>()
        {
            (unknownUser, targetProfile!.Id),
            (firstUser!, Guid.NewGuid()),
            (unknownUser, Guid.NewGuid())
        };
        var actualMessage = "Requested profile was not found";

        foreach (var argument in arguments)
        {
            var throws = Assert.ThrowsAsync<FrontendException>(
                async () => { await _profileOrchestrator.GetUserProfile(argument.Item1, argument.Item2); },
                $"Expected the following exception message: {actualMessage}");
            Assert.That(throws!.Messages.Count(), Is.EqualTo(1));
            Assert.That(throws.Message, Is.EqualTo(actualMessage));
            Assert.That(throws.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount));
    }

    [Test]
    public async Task CreateUserProfileTest()
    {
        var usersDbSet = _dbContextMock.Object.Users;
        var user = usersDbSet.FirstOrDefault() as ApplicationUser;

        var profilesInitialCount = _dbContextMock.Object.Profiles.Count();
        var userProfile = new UserProfile()
        {
            Id = Guid.NewGuid(),
            Title = _random.Next(100000, 999999).ToString()
        };
        var userProfileCopy = userProfile.Clone();
        await _profileOrchestrator.CreateUserProfile(user!, userProfile);

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(profilesInitialCount + 1));
        Assert.That(userProfile.Id, Is.EqualTo(userProfileCopy.Id));
        Assert.That(userProfile.Title, Is.EqualTo(userProfileCopy.Title));
        Assert.That(userProfile.UserId, Is.EqualTo(user!.Id));
    }


    [Test]
    public async Task CreateAndUpdateUserProfileTest()
    {
        var usersDbSet = _dbContextMock.Object.Users;
        var user = usersDbSet.FirstOrDefault() as ApplicationUser;

        var profilesInitialCount = _dbContextMock.Object.Profiles.Count();
        var userProfileId = Guid.NewGuid();
        var userProfile = new UserProfile()
        {
            Id = userProfileId,
            Title = _random.Next(100000, 999999).ToString()
        };
        var userProfileCopy = userProfile.Clone();
        await _profileOrchestrator.CreateUserProfile(user!, userProfile);

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(profilesInitialCount + 1));
        Assert.That(userProfile.Id, Is.EqualTo(userProfileCopy.Id));
        Assert.That(userProfile.Title, Is.EqualTo(userProfileCopy.Title));
        Assert.That(userProfile.UserId, Is.EqualTo(user!.Id));

        var userProfileToUpdate = new UserProfile()
        {
            Id = userProfile.Id,
            Title = _random.Next(100000, 999999).ToString(),
            UserId = userProfile.UserId
        };
        var userProfileToUpdateCopy = userProfileToUpdate.Clone();
        await _profileOrchestrator.UpdateUserProfile(user!, userProfileToUpdate, userProfile);

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(profilesInitialCount + 1));
        Assert.That(userProfileToUpdate.Id, Is.EqualTo(userProfileToUpdateCopy.Id));
        Assert.That(userProfileToUpdate.Title, Is.EqualTo(userProfileToUpdateCopy.Title));
        Assert.That(userProfileToUpdate.Title, Is.Not.EqualTo(userProfileCopy.Title));
        Assert.That(userProfileToUpdate.UserId, Is.EqualTo(user!.Id));
    }

    [Test]
    public async Task CreateAndPartiallyUpdateUserProfileTest()
    {
        var usersDbSet = _dbContextMock.Object.Users;
        var user = usersDbSet.FirstOrDefault() as ApplicationUser;

        var profilesInitialCount = _dbContextMock.Object.Profiles.Count();
        var userProfileId = Guid.NewGuid();
        var userProfile = new UserProfile()
        {
            Id = userProfileId,
            Title = _random.Next(100000, 999999).ToString()
        };

        await _profileOrchestrator.CreateUserProfile(user!, userProfile);
        var userProfileCopy = userProfile.Clone();

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(profilesInitialCount + 1));
        Assert.That(userProfile.Id, Is.EqualTo(userProfileCopy.Id));
        Assert.That(userProfile.Title, Is.EqualTo(userProfileCopy.Title));
        Assert.That(userProfile.UserId, Is.EqualTo(user!.Id));

        var patch = new Delta<UserProfile>();
        patch.TrySetPropertyValue("Title", _random.Next(100000, 999999).ToString());
        var userProfileToUpdate = patch.GetInstance().Clone();
        var userProfileUpdated = await _profileOrchestrator.PartiallyUpdateUserProfile(user!, patch, userProfile);

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(profilesInitialCount + 1));
        Assert.That(userProfileUpdated.Id, Is.EqualTo(userProfileId));
        Assert.That(userProfileUpdated.Title, Is.EqualTo(userProfileToUpdate.Title));
        Assert.That(userProfileUpdated.Title, Is.Not.EqualTo(userProfileCopy.Title));
        Assert.That(userProfileUpdated.UserId, Is.EqualTo(user!.Id));
    }

    [Test]
    public async Task DeleteUserProfileTest()
    {
        var usersDbSet = _dbContextMock.Object.Users;
        var user = usersDbSet.FirstOrDefault() as ApplicationUser;

        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();
        var userProfile = profilesDbSet.FirstOrDefault();

        await _profileOrchestrator.DeleteUserProfile(user!, userProfile!.Id);

        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount - 1));
        var throws = Assert.ThrowsAsync<FrontendException>(
            async () => { await _profileOrchestrator.GetUserProfile(user!, userProfile!.Id); });
        Assert.NotNull(throws);
    }

    [Test]
    public void DeleteUserProfileErrorsTest()
    {
        var usersDbSet = _dbContextMock.Object.Users;
        var user = usersDbSet.FirstOrDefault() as ApplicationUser;
        var unknownUser = new ApplicationUser();

        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();
        var userProfile = profilesDbSet.FirstOrDefault();

        var arguments = new List<(ApplicationUser, Guid)>()
        {
            (unknownUser, userProfile!.Id),
            (user!, Guid.NewGuid()),
            (unknownUser, Guid.NewGuid())
        };
        var actualMessage = "Requested profile was not found";
        foreach (var argument in arguments)
        {
            var throws = Assert.ThrowsAsync<FrontendException>(
                async () => { await _profileOrchestrator.DeleteUserProfile(argument.Item1, argument.Item2); },
                $"Expected the following exception message: {actualMessage}");
            Assert.That(throws!.Messages.Count(), Is.EqualTo(1));
            Assert.That(throws.Message, Is.EqualTo(actualMessage));
            Assert.That(throws.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount));
    }


    private Mock<XSchedDbContext> GetDbContextMock()
    {
        var optionsBuilder = new DbContextOptionsBuilder<XSchedDbContext>();
        optionsBuilder.UseInMemoryDatabase("MyDatabase");

        var dbContextMock = new DbContextMock<XSchedDbContext>(optionsBuilder.Options) { CallBase = true };

        SetupUsersDbSetMock(dbContextMock);
        SetupUserProfilesDbSetMock(dbContextMock);

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

    private Mock<ProfileRepository> GetProfileRepositoryMock()
    {
        var repository = new Mock<ProfileRepository>(_dbContextMock.Object) { CallBase = true };
        return repository;
    }

    private ProfilesOrchestrator GetProfileOrchestrator()
    {
        return new ProfilesOrchestrator(_profileRepositoryMock.Object);
    }
}