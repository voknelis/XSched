﻿using EntityFrameworkCoreMock;
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
using XSched.API.Tests.Helpers;

namespace XSched.API.Tests.Controllers;

public class ProfilesControllerTest
{
    private Random _random;
    private Mock<XSchedDbContext> _dbContextMock;
    private Mock<ProfileRepository> _profileRepositoryMock;
    private Mock<ProfilesOrchestrator> _profileOrchestrator;
    private Mock<ProfilesController> _profilesController;

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
        _profilesController = GetProfilesControllerMock();
        var result = await _profilesController.Object.GetUserProfiles() as OkObjectResult;
        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That((result!.Value as IEnumerable<UserProfile>)!.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetUserProfileTest()
    {
        _profilesController = GetProfilesControllerMock();

        var targetProfile = _dbContextMock.Object.Profiles.FirstOrDefault()!;

        var result = await _profilesController.Object.GetUserProfile(targetProfile.Id) as OkObjectResult;
        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        var userProfile = result.Value as UserProfile;
        Assert.That(userProfile!, Is.EqualTo(targetProfile));
    }

    [Test]
    public async Task CreateUserProfileTest()
    {
        _profilesController = GetProfilesControllerMock();

        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();
        var userProfile = new UserProfile()
        {
            Id = Guid.NewGuid(),
            Title = _random.Next(100000, 999999).ToString()
        };
        var userProfileCopy = userProfile.Clone();
        var user = _dbContextMock.Object.Users.FirstOrDefault()!;

        var result = await _profilesController.Object.CreateUserProfile(userProfile) as CreatedODataResult<UserProfile>;

        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount + 1));
        Assert.NotNull(result);
        Assert.That(result!.Entity.Id, Is.EqualTo(userProfileCopy.Id));
        Assert.That(result!.Entity.Title, Is.EqualTo(userProfileCopy.Title));
        Assert.That(result!.Entity.UserId, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task CreateUserProfileModelValidationErrorTest()
    {
        _profilesController = GetProfilesControllerMock();

        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();
        var userProfile = new UserProfile()
        {
            Id = Guid.NewGuid()
        };
        _profilesController.Object.ModelState.AddModelError("Title", "The Title field is required.");

        var result = await _profilesController.Object.CreateUserProfile(userProfile) as BadRequestObjectResult;

        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount));
        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task CreateUserProfileInvalidUserErrorTest()
    {
        _profilesController = GetProfilesControllerMock(false);

        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();
        var userProfile = new UserProfile();

        var result = await _profilesController.Object.CreateUserProfile(userProfile) as UnauthorizedResult;

        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount));
        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
    }

    [Test]
    public async Task CreateAndUpdateUserProfileTest()
    {
        _profilesController = GetProfilesControllerMock();

        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();
        var userProfile = new UserProfile()
        {
            Id = Guid.NewGuid(),
            Title = _random.Next(100000, 999999).ToString()
        };
        var userProfileCopy = userProfile.Clone();
        var user = _dbContextMock.Object.Users.FirstOrDefault()!;

        var createResult =
            await _profilesController.Object.UpdateUserProfile(userProfile.Id, userProfile) as
                CreatedODataResult<UserProfile>;

        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount + 1));
        Assert.NotNull(createResult);
        Assert.That(createResult!.Entity.Id, Is.EqualTo(userProfileCopy.Id));
        Assert.That(createResult!.Entity.Title, Is.EqualTo(userProfileCopy.Title));
        Assert.That(createResult!.Entity.UserId, Is.EqualTo(user.Id));

        var userProfileToUpdate = new UserProfile()
        {
            Id = userProfile.Id,
            Title = _random.Next(100000, 999999).ToString(),
            UserId = userProfile.UserId
        };
        var userProfileToUpdateCopy = userProfileToUpdate.Clone();
        var updateResult =
            await _profilesController.Object.UpdateUserProfile(userProfileToUpdate.Id, userProfileToUpdate) as
                OkObjectResult;
        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount + 1));

        var userProfileUpdated = updateResult!.Value as UserProfile;
        Assert.That(userProfileUpdated!.Id, Is.EqualTo(userProfileToUpdateCopy.Id));
        Assert.That(userProfileUpdated!.Title, Is.EqualTo(userProfileToUpdateCopy.Title));
        Assert.That(userProfileUpdated!.Title, Is.Not.EqualTo(userProfileCopy.Title));
        Assert.That(userProfileUpdated!.UserId, Is.EqualTo(user.Id));

        _profileOrchestrator.Verify(x => x.CreateUserProfile(It.IsAny<ApplicationUser>(), It.IsAny<UserProfile>()),
            Times.Once);
        _profileOrchestrator.Verify(
            x => x.UpdateUserProfile(It.IsAny<ApplicationUser>(), It.IsAny<UserProfile>(), It.IsAny<UserProfile>()),
            Times.Once);
    }

    [Test]
    public async Task CreateAndUpdateUserProfileInvalidUserErrorTest()
    {
        _profilesController = GetProfilesControllerMock(false);

        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();
        var userProfile = new UserProfile();

        var result =
            await _profilesController.Object.UpdateUserProfile(userProfile.Id, userProfile) as UnauthorizedResult;

        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount));
        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        _profileOrchestrator.Verify(x => x.CreateUserProfile(It.IsAny<ApplicationUser>(), It.IsAny<UserProfile>()),
            Times.Never);
        _profileOrchestrator.Verify(
            x => x.UpdateUserProfile(It.IsAny<ApplicationUser>(), It.IsAny<UserProfile>(), It.IsAny<UserProfile>()),
            Times.Never);
    }

    [Test]
    public async Task CreateAndPartiallyUpdateUserProfileTest()
    {
        _profilesController = GetProfilesControllerMock();

        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();

        var patch = new Delta<UserProfile>();
        patch.TrySetPropertyValue("Id", Guid.NewGuid());
        patch.TrySetPropertyValue("Title", _random.Next(100000, 999999).ToString());
        var userProfile = patch.GetInstance();
        var userProfileCopy = userProfile.Clone();
        var user = _dbContextMock.Object.Users.FirstOrDefault()!;

        var createResult =
            await _profilesController.Object.PartiallyUpdateUserProfile(userProfile.Id, patch) as
                CreatedODataResult<UserProfile>;

        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount + 1));
        Assert.NotNull(createResult);
        Assert.That(createResult!.Entity.Id, Is.EqualTo(userProfileCopy.Id));
        Assert.That(createResult!.Entity.Title, Is.EqualTo(userProfileCopy.Title));
        Assert.That(createResult!.Entity.UserId, Is.EqualTo(user.Id));

        var patchToUpdate = new Delta<UserProfile>();
        patchToUpdate.TrySetPropertyValue("Title", _random.Next(100000, 999999).ToString());
        var userProfileToUpdate = patchToUpdate.GetInstance().Clone();
        var updateResult =
            await _profilesController.Object.PartiallyUpdateUserProfile(userProfile.Id, patchToUpdate) as
                OkObjectResult;
        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount + 1));

        var userProfileUpdated = updateResult!.Value as UserProfile;
        Assert.That(userProfileUpdated!.Id, Is.EqualTo(userProfile.Id));
        Assert.That(userProfileUpdated!.Title, Is.EqualTo(userProfileToUpdate.Title));
        Assert.That(userProfileUpdated!.Title, Is.Not.EqualTo(userProfileCopy.Title));
        Assert.That(userProfileUpdated!.UserId, Is.EqualTo(user.Id));

        _profileOrchestrator.Verify(x => x.CreateUserProfile(It.IsAny<ApplicationUser>(), It.IsAny<UserProfile>()),
            Times.Once);
        _profileOrchestrator.Verify(
            x => x.PartiallyUpdateUserProfile(It.IsAny<ApplicationUser>(), It.IsAny<Delta<UserProfile>>(),
                It.IsAny<UserProfile>()),
            Times.Once);
    }

    [Test]
    public async Task DeleteUserProfileTest()
    {
        _profilesController = GetProfilesControllerMock();

        var profilesDbSet = _dbContextMock.Object.Profiles;
        var profilesInitialCount = profilesDbSet.Count();

        var targetProfile = profilesDbSet.FirstOrDefault()!;
        await _profilesController.Object.DeleteUserProfile(targetProfile.Id);
        var findProfile = profilesDbSet.FirstOrDefault(p => p.Id == targetProfile.Id);

        Assert.That(profilesDbSet.Count(), Is.EqualTo(profilesInitialCount - 1));
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

    private Mock<ProfilesOrchestrator> GetProfileOrchestrator()
    {
        return new Mock<ProfilesOrchestrator>(_profileRepositoryMock.Object) { CallBase = true };
    }

    private Mock<ProfilesController> GetProfilesControllerMock(bool returnUser = true)
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock =
            new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null,
                null);

        var user = returnUser ? _dbContextMock.Object.Users.FirstOrDefault() as ApplicationUser : null;
        var controllerMock = new Mock<ProfilesController>(userManagerMock.Object, _profileOrchestrator.Object)
            { CallBase = true };
        controllerMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user!);
        return controllerMock;
    }
}