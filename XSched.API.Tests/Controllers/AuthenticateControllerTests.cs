using EntityFrameworkCoreMock;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using XSched.API.Controllers;
using XSched.API.DbContexts;
using XSched.API.Dtos;
using XSched.API.Entities;
using XSched.API.Models;
using XSched.API.Orchestrators.Implementations;
using XSched.API.Repositories.Implementation;
using XSched.API.Services.Implementations;
using XSched.API.Services.Interfaces;

namespace XSched.API.Tests.Controllers;

public class AuthenticateControllerTests
{
    private Random _random;
    private Mock<UserManager<ApplicationUser>> _userManagerMock;
    private Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private IJwtTokenService _jwtTokenService;
    private Mock<XSchedDbContext> _dbContextMock;
    private Mock<ProfilesOrchestrator> _profilesOrchestratorMock;
    private Mock<AuthenticateOrchestrator> _authenticateOrchestratorMock;
    private Mock<AuthenticateController> _authenticateControllerMock;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
        _userManagerMock = GetUserManagerMock();
        _roleManagerMock = GetRoleManagerMock();
        _jwtTokenService = GetJwtTokenService();
        _dbContextMock = GetDbContextMock();
        _profilesOrchestratorMock = GetProfilesOrchestratorMock();
        _authenticateOrchestratorMock = GetAuthenticateOrchestratorMock();
        _authenticateControllerMock = GetAuthenticateControllerMock();
    }

    [Test]
    public async Task RegisterTest()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(() => null!);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var registerModel = new RegisterModel()
        {
            Email = "test@email.com",
            Username = "testUser",
            Password = "password"
        };

        var result = await _authenticateControllerMock.Object.Register(registerModel) as OkResult;


        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        _authenticateOrchestratorMock.Verify(x => x.Register(It.IsAny<RegisterModel>()), Times.Once);
    }

    [Test]
    public void RegisterUserExistsErrorTest()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser());
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var registerModel = new RegisterModel();

        var actualMessage = "User already exist";
        var throws = Assert.ThrowsAsync<FrontendException>(
            async () => { await _authenticateControllerMock.Object.Register(registerModel); },
            $"Expected the following exception message: {actualMessage}");


        Assert.NotNull(throws);
        Assert.That(throws!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(throws.Message, Is.EqualTo(actualMessage));

        _authenticateOrchestratorMock.Verify(x => x.Register(It.IsAny<RegisterModel>()), Times.Never);
    }

    [Test]
    public async Task RegisterErrorTest()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(() => null!);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed());

        var registerModel = new RegisterModel()
        {
            Email = "test@email.com",
            Username = "testUser",
            Password = "password"
        };

        var result = await _authenticateControllerMock.Object.Register(registerModel) as ObjectResult;


        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));

        _authenticateOrchestratorMock.Verify(x => x.Register(It.IsAny<RegisterModel>()), Times.Once);
    }

    [Test]
    public async Task LoginTest()
    {
        var user = new ApplicationUser()
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@email.com",
            UserName = "testUser"
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var clientMetadata = new ClientConnectionMetadata()
        {
            Fingerprint = _random.Next(100000, 999999).ToString(),
            UserAgent = _random.Next(100000, 999999).ToString(),
            Ip = _random.Next(100000, 999999).ToString()
        };
        SetupAuthenticateControllerClientMetadata(clientMetadata);

        var loginModel = new LoginModel()
        {
            Username = user.UserName,
            Password = "password",
            Fingerprint = clientMetadata.Fingerprint
        };

        var result = await _authenticateControllerMock.Object.Login(loginModel) as OkObjectResult;


        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        _authenticateOrchestratorMock.Verify(
            x => x.Login(It.IsAny<ApplicationUser>(), It.IsAny<ClientConnectionMetadata>()), Times.Once);
    }

    [Test]
    public void LoginUserNotFoundErrorTest()
    {
        var user = new ApplicationUser();

        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var loginModel = new LoginModel();

        var actualMessage = "Invalid login or password";
        var throws = Assert.ThrowsAsync<FrontendException>(
            async () => { await _authenticateControllerMock.Object.Login(loginModel); },
            $"Expected the following exception message: {actualMessage}");


        Assert.NotNull(throws);
        Assert.That(throws!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(throws.Message, Is.EqualTo(actualMessage));

        _authenticateOrchestratorMock.Verify(
            x => x.Login(It.IsAny<ApplicationUser>(), It.IsAny<ClientConnectionMetadata>()), Times.Never);
    }

    [Test]
    public void LoginUserInvalidPasswordErrorTest()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(() => null!);

        var loginModel = new LoginModel();

        var actualMessage = "User not found";
        var throws = Assert.ThrowsAsync<FrontendException>(
            async () => { await _authenticateControllerMock.Object.Login(loginModel); },
            $"Expected the following exception message: {actualMessage}");


        Assert.NotNull(throws);
        Assert.That(throws!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(throws.Message, Is.EqualTo(actualMessage));

        _authenticateOrchestratorMock.Verify(
            x => x.Login(It.IsAny<ApplicationUser>(), It.IsAny<ClientConnectionMetadata>()), Times.Never);
    }

    [Test]
    public async Task RefreshTokenTest()
    {
        var user = new ApplicationUser()
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@email.com",
            UserName = "testUser"
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var clientMetadata = new ClientConnectionMetadata()
        {
            Fingerprint = _random.Next(100000, 999999).ToString(),
            UserAgent = _random.Next(100000, 999999).ToString(),
            Ip = _random.Next(100000, 999999).ToString()
        };
        SetupAuthenticateControllerClientMetadata(clientMetadata);

        var loginModel = new LoginModel()
        {
            Username = user.UserName,
            Password = "password",
            Fingerprint = clientMetadata.Fingerprint
        };

        var result = await _authenticateControllerMock.Object.Login(loginModel) as OkObjectResult;

        var tokenResult = result!.Value as TokenResponse;

        var refreshTokenModel = new RefreshTokenModel()
        {
            AccessToken = tokenResult!.AccessToken,
            RefreshToken = tokenResult.RefrestToken,
            Fingerprint = clientMetadata.Fingerprint
        };

        var refreshResult = await _authenticateControllerMock.Object.RefreshToken(refreshTokenModel) as OkResult;


        Assert.NotNull(result);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        _authenticateOrchestratorMock.Verify(
            x => x.RefreshToken(It.IsAny<RefreshTokenModel>(), It.IsAny<ClientConnectionMetadata>()), Times.Once);
    }

    [Test]
    public void RefreshTokenModelErrorTest()
    {
        var actualMessage = "Access and refresh token should be specified";
        var throws = Assert.ThrowsAsync<FrontendException>(
            async () => { await _authenticateControllerMock.Object.RefreshToken(null!); },
            $"Expected the following exception message: {actualMessage}");


        Assert.NotNull(throws);
        Assert.That(throws!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(throws.Message, Is.EqualTo(actualMessage));

        _authenticateOrchestratorMock.Verify(
            x => x.RefreshToken(It.IsAny<RefreshTokenModel>(), It.IsAny<ClientConnectionMetadata>()), Times.Never);
    }

    private Mock<UserManager<ApplicationUser>> GetUserManagerMock()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock =
            new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null,
                null);

        userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>() { "Admin", "User" });

        return userManagerMock;
    }

    private Mock<RoleManager<IdentityRole>> GetRoleManagerMock()
    {
        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        var roleManagerMock = new Mock<RoleManager<IdentityRole>>(roleStoreMock.Object, null, null, null, null);

        return roleManagerMock;
    }

    private IJwtTokenService GetJwtTokenService()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            { "JWT:ValidAudience", "http://localhost:4200" },
            { "JWT:ValidIssuer", "http://localhost:5000" },
            { "JWT:Secret", "sometestsecretasreallylongstring" },
            { "JWT:TokenValidityInHours", "3" },
            { "JWT:RefreshTokenValidityInDays", "7" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        return new JwtTokenService(configuration);
    }


    private Mock<XSchedDbContext> GetDbContextMock()
    {
        var optionsBuilder = new DbContextOptionsBuilder<XSchedDbContext>();
        optionsBuilder.UseInMemoryDatabase("MyDatabase");

        var dbContextMock = new DbContextMock<XSchedDbContext>(optionsBuilder.Options) { CallBase = true };

        dbContextMock.CreateDbSetMock(x => x.Users);
        dbContextMock.CreateDbSetMock(x => x.Profiles);
        dbContextMock.CreateDbSetMock(x => x.RefreshSessions);

        return dbContextMock;
    }

    private Mock<AuthenticateOrchestrator> GetAuthenticateOrchestratorMock()
    {
        return new Mock<AuthenticateOrchestrator>(_userManagerMock.Object, _roleManagerMock.Object, _jwtTokenService,
            _dbContextMock.Object) { CallBase = true };
    }

    private Mock<ProfilesOrchestrator> GetProfilesOrchestratorMock()
    {
        var profileRepository = GetProfilesRepositoryMock();
        return new Mock<ProfilesOrchestrator>(profileRepository.Object) { CallBase = true };
    }

    private Mock<ProfileRepository> GetProfilesRepositoryMock()
    {
        var repository = new Mock<ProfileRepository>(_dbContextMock.Object) { CallBase = true };
        return repository;
    }

    private Mock<AuthenticateController> GetAuthenticateControllerMock()
    {
        return new Mock<AuthenticateController>(_userManagerMock.Object, _authenticateOrchestratorMock.Object,
            _profilesOrchestratorMock.Object) { CallBase = true };
    }

    private void SetupAuthenticateControllerClientMetadata(ClientConnectionMetadata metadata)
    {
        _authenticateControllerMock.Setup(x => x.GetClientMeta(It.IsAny<string>())).Returns(metadata);
    }
}