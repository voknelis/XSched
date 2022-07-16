using System.Reflection;
using EntityFrameworkCoreMock;
using Microsoft.EntityFrameworkCore;
using Moq;
using XSched.API.DbContexts;
using XSched.API.Entities;
using XSched.API.Repositories.Implementation;
using XSched.API.Tests.Helpers;

namespace XSched.API.Tests.Repositories;

public class ProfileRepositoryTests
{
    private Random _random;
    private Mock<XSchedDbContext> _dbContextMock;
    private ProfileRepository _profileRepository;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
        _dbContextMock = GetDbContextMock();
        _profileRepository = GetProfileRepository();
    }

    [Test]
    public void GetUserProfilesTest()
    {
        var usersDbSet = _dbContextMock.Object.Users;
        var firstUser = usersDbSet.FirstOrDefault() as ApplicationUser;
        var secondUser = usersDbSet.ToList()[1] as ApplicationUser;

        var profilesFirstUser = _profileRepository.GetUserProfiles(firstUser!.Id);
        var profilesSecondUser = _profileRepository.GetUserProfiles(secondUser!.Id);
        var profilesUnknownUser = _profileRepository.GetUserProfiles(Guid.NewGuid().ToString());

        Assert.That(profilesFirstUser.Count(), Is.EqualTo(1));
        Assert.That(profilesSecondUser.Count(), Is.EqualTo(2));
        Assert.That(profilesUnknownUser.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetUserProfileByIdTest()
    {
        var user = _dbContextMock.Object.Users.FirstOrDefault() as ApplicationUser;

        var targetProfile = _dbContextMock.Object.Profiles.FirstOrDefault()!;

        var repository = _profileRepository;
        var profile = await repository.GetUserProfileByIdAsync(user!.Id, targetProfile.Id);
        Assert.IsNotNull(profile);
        Assert.That(profile, Is.EqualTo(targetProfile));

        profile = await repository.GetUserProfileByIdAsync(Guid.NewGuid().ToString(), targetProfile.Id);
        Assert.IsNull(profile);

        profile = await repository.GetUserProfileByIdAsync(user!.Id, Guid.NewGuid());
        Assert.IsNull(profile);

        profile = await repository.GetUserProfileByIdAsync(Guid.NewGuid().ToString(), Guid.NewGuid());
        Assert.IsNull(profile);
    }

    [Test]
    public async Task GetDefaultUserProfileTest()
    {
        var user = _dbContextMock.Object.Users.Last() as ApplicationUser;

        var targetProfile = _dbContextMock.Object.Profiles.ToList()[1];
        var profile = await _profileRepository.GetDefaultUserProfileAsync(user!.Id);

        Assert.NotNull(profile);
        Assert.That(profile, Is.EqualTo(targetProfile));
    }

    [Test]
    public async Task GetEmptyDefaultUserProfileTest()
    {
        var user = _dbContextMock.Object.Users.Last() as ApplicationUser;

        var defaultProfiles = _dbContextMock.Object.Profiles.Where(p => p.UserId == user!.Id).ToList();
        foreach (var defaultProfile in defaultProfiles) defaultProfile.IsDefault = false;

        var profile = await _profileRepository.GetDefaultUserProfileAsync(user!.Id);

        Assert.Null(profile);
    }

    [Test]
    public void GetMultipleDefaultUserProfilesTest()
    {
        var user = _dbContextMock.Object.Users.Last() as ApplicationUser;

        var defaultProfiles = _dbContextMock.Object.Profiles.Where(p => p.UserId == user!.Id).ToList();
        foreach (var defaultProfile in defaultProfiles) defaultProfile.IsDefault = true;
        _dbContextMock.Object.SaveChanges();

        var throws = Assert.ThrowsAsync<TargetInvocationException>(
            async () => { await _profileRepository.GetDefaultUserProfileAsync(user!.Id); });

        Assert.NotNull(throws);
    }

    [Test]
    public void CreateProfileTest()
    {
        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();
        var user = _dbContextMock.Object.Users.FirstOrDefault() as ApplicationUser;

        var userProfile = new UserProfile()
        {
            Id = Guid.NewGuid(),
            Title = _random.Next(100000, 999999).ToString(),
            UserId = user!.Id
        };

        _profileRepository.CreateProfile(userProfile);
        _dbContextMock.Object.SaveChanges();
        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount + 1));
    }

    [Test]
    public void UpdateProfile()
    {
        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();
        var user = _dbContextMock.Object.Users.FirstOrDefault() as ApplicationUser;

        var userProfile = new UserProfile()
        {
            Id = Guid.NewGuid(),
            Title = _random.Next(100000, 999999).ToString(),
            UserId = user!.Id
        };

        var repository = _profileRepository;
        repository.CreateProfile(userProfile);
        _dbContextMock.Object.SaveChanges();
        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount + 1));

        var userProfileCopy = userProfile.Clone();
        var userProfileUpdated = userProfile.Clone();

        userProfileUpdated.Title = _random.Next(100000, 999999).ToString();
        repository.UpdateProfile(userProfile, userProfileUpdated);
        _dbContextMock.Object.SaveChanges();

        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount + 1));
        Assert.That(userProfileUpdated.Id, Is.EqualTo(userProfile.Id));
        Assert.That(userProfileUpdated.Title, Is.EqualTo(userProfile.Title));
        Assert.That(userProfileUpdated.UserId, Is.EqualTo(userProfile.UserId));

        Assert.That(userProfileCopy.Id, Is.EqualTo(userProfile.Id));
        Assert.That(userProfileCopy.Title, Is.Not.EqualTo(userProfile.Title));
        Assert.That(userProfileCopy.UserId, Is.EqualTo(userProfile.UserId));
    }

    [Test]
    public void DeleteProfile()
    {
        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();

        var targetProfile = profilesDbSet.FirstOrDefault()!;

        _profileRepository.DeleteProfile(targetProfile);
        _dbContextMock.Object.SaveChanges();
        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount - 1));

        var findProfile = profilesDbSet.FirstOrDefault(p => p.Id == targetProfile.Id);
        Assert.Null(findProfile);
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
        var firstUser = dbContextMock.Object.Users.FirstOrDefault() as ApplicationUser;
        var secondUser = dbContextMock.Object.Users.ToList()[1] as ApplicationUser;
        var userProfiles = new List<UserProfile>()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = _random.Next(100000, 999999).ToString(),
                User = firstUser,
                UserId = firstUser!.Id,
                IsDefault = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = _random.Next(100000, 999999).ToString(),
                User = secondUser,
                UserId = secondUser!.Id,
                IsDefault = true
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

    private ProfileRepository GetProfileRepository()
    {
        return new ProfileRepository(_dbContextMock.Object);
    }
}