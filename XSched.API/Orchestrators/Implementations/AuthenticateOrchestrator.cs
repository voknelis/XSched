using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using XSched.API.DbContexts;
using XSched.API.Dtos;
using XSched.API.Entities;
using XSched.API.Helpers;
using XSched.API.Models;
using XSched.API.Orchestrators.Interfaces;
using XSched.API.Services.Interfaces;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace XSched.API.Orchestrators.Implementations;

public class AuthenticateOrchestrator : IAuthenticateOrchestrator
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtTokenService _tokenService;
    private readonly XSchedDbContext _dbContext;

    public AuthenticateOrchestrator(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IJwtTokenService tokenService,
        XSchedDbContext dbContext)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }


    public async Task<IdentityResult> Register(RegisterModel model)
    {
        var newUser = new ApplicationUser()
        {
            UserName = model.Username,
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        return await _userManager.CreateAsync(newUser, model.Password);
    }

    public async Task<TokenResponse> Login(ApplicationUser user, ClientConnectionMetadata clientMeta)
    {
        var userRoles = await _userManager.GetRolesAsync(user);

        var authClaims = new List<Claim>()
        {
            new(ClaimTypes.Name, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var userRole in userRoles) authClaims.Add(new Claim(ClaimTypes.Role, userRole));

        var accessToken = _tokenService.GenerateAccessToken(authClaims);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var refreshSession = new RefreshSession()
        {
            UserId = user.Id,
            RefreshToken = refreshToken.Token,
            Created = DateTime.Now,
            ExpiresIn = refreshToken.ExpiresIn,
            Fingerprint = clientMeta.Fingerprint,
            UserAgent = clientMeta.UserAgent,
            Ip = clientMeta.Ip
        };
        _dbContext.RefreshSessions.Add(refreshSession);
        await _dbContext.SaveChangesAsync();

        return new TokenResponse()
        {
            AccessToken = accessToken.TokenString,
            RefrestToken = refreshToken.Token,
            Expiration = accessToken.Token.ValidTo
        };
    }

    public async Task<TokenResponse> RefreshToken(RefreshTokenModel model, ClientConnectionMetadata clientMeta)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(model.AccessToken);
        if (principal == null) throw new FrontendException("Invalid access token");

        var userName = principal.Identity.Name;
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null) throw new FrontendException("User not found");

        var refreshSession = await _dbContext.RefreshSessions.FirstOrDefaultAsync(s =>
            s.RefreshToken == model.RefreshToken && s.Fingerprint == model.Fingerprint);
        if (refreshSession is null) throw new FrontendException("Invalid refresh session");
        if (refreshSession.ExpiresIn <= DateTime.Now) throw new FrontendException("Refresh token has expired");

        var accessToken = _tokenService.GenerateAccessToken(principal.Claims);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _dbContext.RefreshSessions.Remove(refreshSession);

        var newRefreshSession = new RefreshSession()
        {
            UserId = user.Id,
            RefreshToken = refreshToken.Token,
            Created = DateTime.Now,
            ExpiresIn = refreshToken.ExpiresIn,
            Fingerprint = clientMeta.Fingerprint,
            UserAgent = clientMeta.UserAgent,
            Ip = clientMeta.Ip
        };
        _dbContext.RefreshSessions.Add(newRefreshSession);
        await _dbContext.SaveChangesAsync();

        return new TokenResponse()
        {
            AccessToken = accessToken.TokenString,
            RefrestToken = refreshToken.Token,
            Expiration = accessToken.Token.ValidTo
        };
    }
}