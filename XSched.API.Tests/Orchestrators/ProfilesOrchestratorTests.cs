using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using XSched.API.DbContexts;
using XSched.API.Entities;
using XSched.API.Models;
using XSched.API.Orchestrators.Implementations;
using XSched.API.Repositories.Implementation;

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
        Assert.That(profilesDbSet.Count(), Is.EqualTo(3));

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
        Assert.That(profilesDbSet.Count(), Is.EqualTo(3));

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
    }

    [Test]
    public async Task CreateUserProfileTest()
    {
        foreach (var profile in new List<UserProfile>(_dbContextMock.Object.Profiles))
            _dbContextMock.Object.Profiles.Remove(profile);
        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(0));

        var usersDbSet = _dbContextMock.Object.Users;
        var user = usersDbSet.FirstOrDefault() as ApplicationUser;

        var userProfile = new UserProfile()
        {
            Id = Guid.NewGuid(),
            Title = _random.Next(100000, 999999).ToString()
        };
        var userProfileCopy = Helpers.Helpers.CloneJson(userProfile);
        await _profileOrchestrator.CreateUserProfile(user!, userProfile);

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(1));
        Assert.That(userProfile.Id, Is.EqualTo(userProfileCopy.Id));
        Assert.That(userProfile.Title, Is.EqualTo(userProfileCopy.Title));
        Assert.That(userProfile.UserId, Is.EqualTo(user!.Id));
    }


    [Test]
    public async Task CreateAndUpdateUserProfileTest()
    {
        foreach (var profile in new List<UserProfile>(_dbContextMock.Object.Profiles))
            _dbContextMock.Object.Profiles.Remove(profile);
        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(0));

        var usersDbSet = _dbContextMock.Object.Users;
        var user = usersDbSet.FirstOrDefault() as ApplicationUser;

        var userProfileId = Guid.NewGuid();
        var userProfile = new UserProfile()
        {
            Id = userProfileId,
            Title = _random.Next(100000, 999999).ToString()
        };
        var userProfileCopy = Helpers.Helpers.CloneJson(userProfile);
        await _profileOrchestrator.UpdateUserProfile(user!, userProfile, userProfileId);

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(1));
        Assert.That(userProfile.Id, Is.EqualTo(userProfileCopy.Id));
        Assert.That(userProfile.Title, Is.EqualTo(userProfileCopy.Title));
        Assert.That(userProfile.UserId, Is.EqualTo(user!.Id));

        userProfile.Title = _random.Next(100000, 999999).ToString();
        await _profileOrchestrator.UpdateUserProfile(user!, userProfile, userProfileId);

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(1));
        Assert.That(userProfile.Id, Is.EqualTo(userProfileCopy.Id));
        Assert.That(userProfile.Title, Is.Not.EqualTo(userProfileCopy.Title));
        Assert.That(userProfile.UserId, Is.EqualTo(user!.Id));
    }

    [Test]
    public async Task CreateAndPartiallyUpdateUserProfileTest()
    {
        foreach (var profile in new List<UserProfile>(_dbContextMock.Object.Profiles))
            _dbContextMock.Object.Profiles.Remove(profile);
        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(0));

        var usersDbSet = _dbContextMock.Object.Users;
        var user = usersDbSet.FirstOrDefault() as ApplicationUser;

        var userProfileId = Guid.NewGuid();

        var patch = new Delta<UserProfile>();
        patch.TrySetPropertyValue("Id", userProfileId);
        patch.TrySetPropertyValue("Title", _random.Next(100000, 999999).ToString());

        var userProfile = await _profileOrchestrator.PartiallyUpdateUserProfile(user!, patch, userProfileId);
        var userProfileCopy = Helpers.Helpers.CloneJson(userProfile);

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(1));
        Assert.That(userProfile.Id, Is.EqualTo(userProfileCopy.Id));
        Assert.That(userProfile.Title, Is.EqualTo(userProfileCopy.Title));
        Assert.That(userProfile.UserId, Is.EqualTo(user!.Id));

        patch.TrySetPropertyValue("Title", _random.Next(100000, 999999).ToString());
        userProfile = await _profileOrchestrator.PartiallyUpdateUserProfile(user!, patch, userProfileId);

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(1));
        Assert.That(userProfile.Id, Is.EqualTo(userProfileCopy.Id));
        Assert.That(userProfile.Title, Is.Not.EqualTo(userProfileCopy.Title));
        Assert.That(userProfile.UserId, Is.EqualTo(user!.Id));
    }

    [Test]
    public async Task DeleteUserProfileTest()
    {
        var usersDbSet = _dbContextMock.Object.Users;
        var user = usersDbSet.FirstOrDefault() as ApplicationUser;

        var profilesDbSet = _dbContextMock.Object.Profiles;
        var userProfile = profilesDbSet.FirstOrDefault();

        Assert.That(profilesDbSet.Count(), Is.EqualTo(3));
        await _profileOrchestrator.DeleteUserProfile(user!, userProfile!.Id);

        Assert.That(profilesDbSet.Count(), Is.EqualTo(2));
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
        var userProfile = profilesDbSet.FirstOrDefault();

        Assert.That(profilesDbSet.Count(), Is.EqualTo(3));

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
    }


    private Mock<XSchedDbContext> GetDbContextMock()
    {
        var options = new DbContextOptions<XSchedDbContext>();
        var dbContextMock = new Mock<XSchedDbContext>(options) { CallBase = false };

        SetupUsersDbSetMock(dbContextMock);
        SetupUserProfilesDbSetMock(dbContextMock);

        return dbContextMock;
    }

    private void SetupUsersDbSetMock(Mock<XSchedDbContext> dbContextMock)
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
        dbContextMock.Setup(x => x.Users).ReturnsDbSet(users);
    }

    private void SetupUserProfilesDbSetMock(Mock<XSchedDbContext> dbContextMock)
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

        dbContextMock.Setup(x => x.Profiles).ReturnsDbSet(userProfiles);
        dbContextMock.Setup(x => x.Profiles.Add(It.IsAny<UserProfile>()))
            .Callback<UserProfile>(userProfiles.Add);
        dbContextMock.Setup(x => x.Profiles.AddRange(It.IsAny<IEnumerable<UserProfile>>()))
            .Callback<IEnumerable<UserProfile>>(userProfiles.AddRange);
        dbContextMock.Setup(x => x.Profiles.Remove(It.IsAny<UserProfile>()))
            .Callback<UserProfile>(x => userProfiles.Remove(x));
    }

    private Mock<ProfileRepository> GetProfileRepositoryMock()
    {
        var repository = new Mock<ProfileRepository>(_dbContextMock.Object);
        repository.Setup(x => x.UpdateProfile(It.IsAny<UserProfile>(), It.IsAny<UserProfile>()))
            .Callback<UserProfile, UserProfile>((profileDb, profile) =>
            {
                profileDb.Id = profile.Id;
                profileDb.Title = profile.Title;
                profileDb.UserId = profile.UserId;
            });
        return repository;
    }

    private ProfilesOrchestrator GetProfileOrchestrator()
    {
        return new ProfilesOrchestrator(_profileRepositoryMock.Object);
    }
}