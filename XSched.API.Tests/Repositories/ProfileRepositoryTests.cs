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
    private Mock<ProfileRepository> _profileRepositoryMock;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
        _dbContextMock = GetDbContextMock();
        _profileRepositoryMock = GetProfileRepositoryMock();
    }

    [Test]
    public void GetUserProfilesTest()
    {
        var firstUser = _dbContextMock.Object.Users.FirstOrDefault() as ApplicationUser;
        var secondUser = _dbContextMock.Object.Users.ToList()[1] as ApplicationUser;
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
        var profilesDbSet = _dbContextMock.Object.Profiles;
        profilesDbSet.AddRange(userProfiles);
        _dbContextMock.Object.SaveChanges();

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(3));

        var profilesFirstUser = _profileRepositoryMock.Object.GetUserProfiles(firstUser!.Id);
        var profilesSecondUser = _profileRepositoryMock.Object.GetUserProfiles(secondUser!.Id);
        var profilesUnknownUser = _profileRepositoryMock.Object.GetUserProfiles(Guid.NewGuid().ToString());

        Assert.That(profilesFirstUser.Count(), Is.EqualTo(1));
        Assert.That(profilesSecondUser.Count(), Is.EqualTo(2));
        Assert.That(profilesUnknownUser.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetUserProfileByIdTest()
    {
        var user = _dbContextMock.Object.Users.FirstOrDefault() as ApplicationUser;

        var profileGuid = Guid.NewGuid();
        var userProfiles = new List<UserProfile>()
        {
            new()
            {
                Id = profileGuid,
                Title = _random.Next(100000, 999999).ToString(),
                User = user,
                UserId = user!.Id
            }
        };
        _dbContextMock.Object.Profiles.AddRange(userProfiles);
        _dbContextMock.Object.SaveChanges();

        var repository = _profileRepositoryMock.Object;
        var profile = await repository.GetUserProfileByIdAsync(user!.Id, profileGuid);
        Assert.IsNotNull(profile);
        Assert.That(profile!.Id, Is.EqualTo(profileGuid));

        profile = await repository.GetUserProfileByIdAsync(Guid.NewGuid().ToString(), profileGuid);
        Assert.IsNull(profile);

        profile = await repository.GetUserProfileByIdAsync(user!.Id, Guid.NewGuid());
        Assert.IsNull(profile);

        profile = await repository.GetUserProfileByIdAsync(Guid.NewGuid().ToString(), Guid.NewGuid());
        Assert.IsNull(profile);
    }

    [Test]
    public void CreateProfileTest()
    {
        var user = _dbContextMock.Object.Users.FirstOrDefault() as ApplicationUser;

        var userProfile = new UserProfile()
        {
            Id = Guid.NewGuid(),
            Title = _random.Next(100000, 999999).ToString(),
            UserId = user!.Id
        };

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(0));

        var repository = _profileRepositoryMock.Object;
        repository.CreateProfile(userProfile);
        _dbContextMock.Object.SaveChanges();
        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(1));
    }

    [Test]
    public void UpdateProfile()
    {
        var user = _dbContextMock.Object.Users.FirstOrDefault() as ApplicationUser;

        var userProfile = new UserProfile()
        {
            Id = Guid.NewGuid(),
            Title = _random.Next(100000, 999999).ToString(),
            UserId = user!.Id
        };

        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(0));

        var repository = _profileRepositoryMock.Object;
        repository.CreateProfile(userProfile);
        _dbContextMock.Object.SaveChanges();
        Assert.That(_dbContextMock.Object.Profiles.Count(), Is.EqualTo(1));

        var userProfileCopy = userProfile.Clone();
        var userProfileUpdated = userProfile.Clone();

        userProfileUpdated.Title = _random.Next(100000, 999999).ToString();
        repository.UpdateProfile(userProfile, userProfileUpdated);
        _dbContextMock.Object.SaveChanges();

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
        var user = _dbContextMock.Object.Users.FirstOrDefault() as ApplicationUser;

        var userProfile =
            new UserProfile()
            {
                Id = Guid.NewGuid(),
                Title = _random.Next(100000, 999999).ToString(),
                User = user,
                UserId = user!.Id
            };
        var profilesDbSet = _dbContextMock.Object.Profiles;
        profilesDbSet.Add(userProfile);
        _dbContextMock.Object.SaveChanges();

        var repository = _profileRepositoryMock.Object;

        Assert.That(profilesDbSet.Count(), Is.EqualTo(1));
        repository.DeleteProfile(userProfile);
        _dbContextMock.Object.SaveChanges();
        Assert.That(profilesDbSet.Count(), Is.EqualTo(0));
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

    private void SetupUserProfilesDbSetMock(DbContextMock<XSchedDbContext> dbContextMock)
    {
        var userProfiles = new List<UserProfile>();
        dbContextMock.CreateDbSetMock(x => x.Profiles, userProfiles);
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

    private Mock<ProfileRepository> GetProfileRepositoryMock()
    {
        var repository = new Mock<ProfileRepository>(_dbContextMock.Object) { CallBase = true };
        return repository;
    }
}