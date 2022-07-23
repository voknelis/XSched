using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using XSched.API.Dtos;
using XSched.API.Entities;
using XSched.API.Models;
using XSched.API.Orchestrators.Interfaces;
using XSched.API.Services.Interfaces;

namespace XSched.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticateController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthenticateOrchestrator _authenticateOrchestrator;
    private readonly IProfilesOrchestrator _profilesOrchestrator;

    public AuthenticateController(
        UserManager<ApplicationUser> userManager,
        IAuthenticateOrchestrator authenticateOrchestrator,
        IProfilesOrchestrator profilesOrchestrator)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _authenticateOrchestrator = authenticateOrchestrator ??
                                    throw new ArgumentNullException(nameof(authenticateOrchestrator));
        _profilesOrchestrator = profilesOrchestrator ?? throw new ArgumentNullException(nameof(profilesOrchestrator));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var userExists = await _userManager.FindByNameAsync(model.Username);
        if (userExists != null) throw new FrontendException("User already exist");

        var tupleResult = await _authenticateOrchestrator.Register(model);
        var result = tupleResult.Item1;
        var newUser = tupleResult.Item2;

        if (!result.Succeeded) return StatusCode(StatusCodes.Status500InternalServerError, result.Errors);

        try
        {
            // create default user profile
            var defaultProfile = new UserProfile()
            {
                Title = "Default profile",
                UserId = newUser.Id,
                IsDefault = true
            };
            await _profilesOrchestrator.CreateUserProfile(newUser, defaultProfile);
        }
        catch
        {
            // TODO: Add logger
        }

        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null) throw new FrontendException("User not found");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!isPasswordValid) throw new FrontendException("Invalid login or password");

        var clientMeta = GetClientMeta(model.Fingerprint);
        var tokenResponse = await _authenticateOrchestrator.Login(user, clientMeta);
        return Ok(tokenResponse);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model)
    {
        if (model == null) throw new FrontendException("Access and refresh token should be specified");

        var clientMeta = GetClientMeta(model.Fingerprint);
        var tokenResponse = await _authenticateOrchestrator.RefreshToken(model, clientMeta);
        return Ok(tokenResponse);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    public virtual ClientConnectionMetadata GetClientMeta(string fingerprint)
    {
        return new ClientConnectionMetadata()
        {
            Fingerprint = fingerprint,
            UserAgent = HttpContext.Request.Headers["User-Agent"],
            Ip = HttpContext.Connection.RemoteIpAddress.ToString()
        };
    }
}