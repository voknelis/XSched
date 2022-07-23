using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using XSched.API.Helpers;
using XSched.API.Services.Implementations;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace XSched.API.Tests.Services;

public class JwtTokenServiceTests
{
    private Random _random;
    private IConfiguration _configuration;
    private JwtTokenService _jwtTokenService;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
        _configuration = GetConfiguration();
        _jwtTokenService = GetJwtTokenService();
    }

    [Test]
    public void GenerateAccessTokenTest()
    {
        var userName = _random.Next(100000, 999999).ToString();
        var claimList = new List<Claim>()
        {
            new(ClaimTypes.Name, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var result = _jwtTokenService.GenerateAccessToken(claimList);

        Assert.NotNull(result.Token);
        Assert.NotNull(result.TokenString);
    }

    [Test]
    public void GenerateRefreshTokenTest()
    {
        var result = _jwtTokenService.GenerateRefreshToken();

        Assert.NotNull(result.Token);
        Assert.That(result.ExpiresIn, Is.EqualTo(DateTime.Now.AddDays(7)).Within(1).Seconds);
    }

    [Test]
    public void GetPrincipalFromExpiredTokenTest()
    {
        var userName = _random.Next(100000, 999999).ToString();
        var claimList = new List<Claim>()
        {
            new(ClaimTypes.Name, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenResult = _jwtTokenService.GenerateAccessToken(claimList);
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(tokenResult.TokenString);

        Assert.NotNull(principal);
    }

    [Test]
    public void GetPrincipalFromTokenWithDifferentAlgorithmErrorTest()
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("sometestsecretasreallylongstring"));
        var token = new JwtSecurityToken(
            "http://localhost:5000",
            "http://localhost:4200",
            expires: DateTime.Now.AddHours(3),
            claims: new List<Claim>(),
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha384));
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var throws = Assert.Throws<SecurityTokenException>(() =>
        {
            _jwtTokenService.GetPrincipalFromExpiredToken(tokenString);
        });

        Assert.NotNull(throws);
    }


    private IConfiguration GetConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            { "JWT:ValidAudience", "http://localhost:4200" },
            { "JWT:ValidIssuer", "http://localhost:5000" },
            { "JWT:Secret", "sometestsecretasreallylongstring" },
            { "JWT:TokenValidityInHours", "3" },
            { "JWT:RefreshTokenValidityInDays", "7" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    private JwtTokenService GetJwtTokenService()
    {
        return new JwtTokenService(_configuration);
    }
}