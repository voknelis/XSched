using EntityFrameworkCoreMock;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using XSched.API.DbContexts;
using XSched.API.Dtos;
using XSched.API.Entities;
using XSched.API.Models;
using XSched.API.Orchestrators.Implementations;
using XSched.API.Services.Implementations;
using XSched.API.Services.Interfaces;
using XSched.API.Tests.Helpers;

namespace XSched.API.Tests.Orchestrators;

public class AuthenticateOrchestratorTests
{
    private Random _random;
    private Mock<UserManager<ApplicationUser>> _userManagerMock;
    private Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private IJwtTokenService _jwtTokenService;
    private Mock<XSchedDbContext> _dbContextMock;
    private AuthenticateOrchestrator _orchestrator;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
        _userManagerMock = GetUserManagerMock();
        _roleManagerMock = GetRoleManagerMock();
        _jwtTokenService = GetJwtTokenService();
        _dbContextMock = GetDbContextMock();
        _orchestrator = GetAuthenticateOrchestrator();
    }

    [Test]
    public async Task RegisterTest()
    {
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var registerModel = new RegisterModel()
        {
            Email = "test@email.com",
            Username = "testUser",
            Password = "password"
        };

        var tupleResult = await _orchestrator.Register(registerModel);
        var result = tupleResult.Item1;
        var newUser = tupleResult.Item2;


        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once());
        Assert.True(result.Succeeded);
        Assert.That(newUser.UserName, Is.EqualTo(registerModel.Username));
        Assert.That(newUser.Email, Is.EqualTo(registerModel.Email));
        Assert.NotNull(newUser.SecurityStamp);
    }

    [Test]
    public async Task LoginTest()
    {
        var dbContext = _dbContextMock.Object;
        var sessionsDbSet = _dbContextMock.Object.RefreshSessions;

        var user = new ApplicationUser()
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@email.com",
            UserName = "testUser"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var clientMetadata = new ClientConnectionMetadata()
        {
            Fingerprint = _random.Next(100000, 999999).ToString(),
            UserAgent = _random.Next(100000, 999999).ToString(),
            Ip = _random.Next(100000, 999999).ToString()
        };

        Assert.That(sessionsDbSet.Count(), Is.EqualTo(0));

        var tokenResult = await _orchestrator.Login(user, clientMetadata);


        Assert.NotNull(tokenResult.AccessToken);
        Assert.NotNull(tokenResult.RefrestToken);
        Assert.That(tokenResult.Expiration, Is.EqualTo(DateTime.UtcNow.AddHours(3)).Within(2).Seconds);

        _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Once());
        Assert.That(sessionsDbSet.Count(), Is.EqualTo(1));

        var refreshSession = sessionsDbSet.First();
        Assert.NotNull(refreshSession);

        Assert.That(refreshSession.UserId, Is.EqualTo(user.Id));
        Assert.That(refreshSession.RefreshToken, Is.EqualTo(tokenResult.RefrestToken));
        Assert.That(refreshSession.Created, Is.EqualTo(DateTime.Now).Within(1).Seconds);
        Assert.That(refreshSession.ExpiresIn, Is.EqualTo(DateTime.Now.AddDays(7)).Within(1).Seconds);
        Assert.That(refreshSession.Fingerprint, Is.EqualTo(clientMetadata.Fingerprint));
        Assert.That(refreshSession.UserAgent, Is.EqualTo(clientMetadata.UserAgent));
        Assert.That(refreshSession.Ip, Is.EqualTo(clientMetadata.Ip));
    }

    [Test]
    public async Task RefreshTokenTest()
    {
        var dbContext = _dbContextMock.Object;
        var sessionsDbSet = _dbContextMock.Object.RefreshSessions;

        var user = new ApplicationUser()
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@email.com",
            UserName = "testUser"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        var clientMetadata = new ClientConnectionMetadata()
        {
            Fingerprint = _random.Next(100000, 999999).ToString(),
            UserAgent = _random.Next(100000, 999999).ToString(),
            Ip = _random.Next(100000, 999999).ToString()
        };

        var tokenResult = await _orchestrator.Login(user, clientMetadata);
        var refreshSession = sessionsDbSet.First();
        var refreshSessionCopy = refreshSession.Clone();

        var refreshTokenModel = new RefreshTokenModel()
        {
            Fingerprint = clientMetadata.Fingerprint,
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefrestToken
        };
        Thread.Sleep(1000);

        var refreshTokenResult = await _orchestrator.RefreshToken(refreshTokenModel, clientMetadata);


        Assert.NotNull(refreshTokenResult.AccessToken);
        Assert.NotNull(refreshTokenResult.RefrestToken);
        Assert.That(refreshTokenResult.Expiration, Is.EqualTo(DateTime.UtcNow.AddHours(3)).Within(1).Seconds);

        Assert.That(tokenResult.AccessToken, Is.Not.EqualTo(refreshTokenResult.AccessToken));
        Assert.That(tokenResult.RefrestToken, Is.Not.EqualTo(refreshTokenResult.RefrestToken));
        Assert.That(tokenResult.Expiration, Is.Not.EqualTo(refreshTokenResult.Expiration));

        Assert.That(sessionsDbSet.Count(), Is.EqualTo(1));
        var newRefreshSession = sessionsDbSet.First();
        Assert.NotNull(newRefreshSession);

        Assert.That(newRefreshSession.UserId, Is.EqualTo(user.Id));
        Assert.That(newRefreshSession.RefreshToken, Is.EqualTo(refreshTokenResult.RefrestToken));
        Assert.That(newRefreshSession.Created, Is.EqualTo(DateTime.Now).Within(1).Seconds);
        Assert.That(newRefreshSession.ExpiresIn, Is.EqualTo(DateTime.Now.AddDays(7)).Within(1).Seconds);
        Assert.That(newRefreshSession.Fingerprint, Is.EqualTo(clientMetadata.Fingerprint));
        Assert.That(newRefreshSession.UserAgent, Is.EqualTo(clientMetadata.UserAgent));
        Assert.That(newRefreshSession.Ip, Is.EqualTo(clientMetadata.Ip));

        Assert.That(refreshSessionCopy.RefreshToken, Is.Not.EqualTo(newRefreshSession.RefreshToken));
    }

    [Test]
    public async Task RefreshTokenUserNotFoundErrorTest()
    {
        var user = new ApplicationUser()
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@email.com",
            UserName = "testUser"
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(() => null!);

        var clientMetadata = new ClientConnectionMetadata();
        var tokenResult = await _orchestrator.Login(user, clientMetadata);

        var refreshTokenModel = new RefreshTokenModel()
        {
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefrestToken
        };

        var actualMessage = "User not found";
        var throws = Assert.ThrowsAsync<FrontendException>(
            async () => { await _orchestrator.RefreshToken(refreshTokenModel, clientMetadata); },
            $"Expected the following exception message: {actualMessage}");

        Assert.NotNull(throws);
        Assert.That(throws!.Message, Is.EqualTo(actualMessage));
    }

    [Test]
    public async Task RefreshTokenRefreshSessionNotFoundErrorTest()
    {
        var user = new ApplicationUser()
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@email.com",
            UserName = "testUser"
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        var clientMetadata = new ClientConnectionMetadata();
        var tokenResult = await _orchestrator.Login(user, clientMetadata);

        var refreshTokenModel = new RefreshTokenModel()
        {
            AccessToken = tokenResult.AccessToken,
            RefreshToken = _random.Next(100000, 999999).ToString()
        };

        var actualMessage = "Invalid refresh session";
        var throws = Assert.ThrowsAsync<FrontendException>(
            async () => { await _orchestrator.RefreshToken(refreshTokenModel, clientMetadata); },
            $"Expected the following exception message: {actualMessage}");

        Assert.NotNull(throws);
        Assert.That(throws!.Message, Is.EqualTo(actualMessage));
    }

    [Test]
    public async Task RefreshTokenRefreshSessionExpiredErrorTest()
    {
        var user = new ApplicationUser()
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@email.com",
            UserName = "testUser"
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        var clientMetadata = new ClientConnectionMetadata();
        var tokenResult = await _orchestrator.Login(user, clientMetadata);

        var refreshTokenModel = new RefreshTokenModel()
        {
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefrestToken
        };

        var newRefreshSession = _dbContextMock.Object.RefreshSessions.First();
        newRefreshSession.ExpiresIn = newRefreshSession.ExpiresIn.AddDays(-8);
        await _dbContextMock.Object.SaveChangesAsync();

        var actualMessage = "Refresh token has expired";
        var throws = Assert.ThrowsAsync<FrontendException>(
            async () => { await _orchestrator.RefreshToken(refreshTokenModel, clientMetadata); },
            $"Expected the following exception message: {actualMessage}");

        Assert.NotNull(throws);
        Assert.That(throws!.Message, Is.EqualTo(actualMessage));
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
        dbContextMock.CreateDbSetMock(x => x.RefreshSessions);

        return dbContextMock;
    }

    private AuthenticateOrchestrator GetAuthenticateOrchestrator()
    {
        return new AuthenticateOrchestrator(_userManagerMock.Object, _roleManagerMock.Object, _jwtTokenService,
            _dbContextMock.Object);
    }
}