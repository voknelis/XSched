using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
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
    private readonly IConfiguration _configuration;
    private readonly IJwtTokenService _tokenService;

    public AuthenticateOrchestrator(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        IJwtTokenService tokenService)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
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

    public async Task<TokenResponse> Login(ApplicationUser user)
    {
        var userRoles = await _userManager.GetRolesAsync(user);

        var authClaims = new List<Claim>()
        {
            new(ClaimTypes.Name, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var userRole in userRoles) authClaims.Add(new Claim(ClaimTypes.Role, userRole));

        var accessToken = _tokenService.GenerateAccessToken(authClaims);
        var accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var refreshTokenValidityInDays = _configuration.GetJwtRefreshTokenValidity();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.Now.AddDays(refreshTokenValidityInDays);

        await _userManager.UpdateAsync(user);

        return new TokenResponse()
        {
            AccessToken = accessTokenString,
            RefrestToken = refreshToken,
            Expiration = accessToken.ValidTo
        };
    }

    public async Task<TokenResponse> RefreshToken(RefreshTokenModel model)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(model.AccessToken);
        if (principal == null) throw new FrontendException("Invalid access token");

        var userName = principal.Identity.Name;
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null) throw new FrontendException("User not found");
        if (user.RefreshToken != model.RefreshToken) throw new FrontendException("Invalid refresh token");
        if (user.RefreshTokenExpiry <= DateTime.Now) throw new FrontendException("Refresh token has expired");

        var accessToken = _tokenService.GenerateAccessToken(principal.Claims);
        var accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        await _userManager.UpdateAsync(user);

        return new TokenResponse()
        {
            AccessToken = accessTokenString,
            RefrestToken = refreshToken,
            Expiration = accessToken.ValidTo
        };
    }
}