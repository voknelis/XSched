using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using XSched.API.Entities;
using XSched.API.Models;
using XSched.API.Orchestrators.Interfaces;

namespace XSched.API.Controllers;

[Authorize]
[Route("odata")]
public class ProfilesController : ODataController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProfilesOrchestrator _profilesOrchestrator;

    public ProfilesController(UserManager<ApplicationUser> userManager, IProfilesOrchestrator profilesOrchestrator)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _profilesOrchestrator = profilesOrchestrator ?? throw new ArgumentNullException(nameof(profilesOrchestrator));
    }

    [HttpGet("profiles")]
    [EnableQuery]
    public async Task<IActionResult> GetUserProfiles()
    {
        var user = await GetCurrentUser();

        return Ok(await _profilesOrchestrator.GetUserProfiles(user));
    }

    [HttpGet("profiles({profileId})")]
    [EnableQuery]
    public async Task<IActionResult> GetUserProfile(Guid profileId)
    {
        var user = await GetCurrentUser();

        return Ok(await _profilesOrchestrator.GetUserProfile(user, profileId));
    }

    [HttpPost("profiles")]
    [EnableQuery]
    public async Task<IActionResult> CreateUserProfile([FromBody] UserProfile profile)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUser();
        if (user == null) return Unauthorized();

        await _profilesOrchestrator.CreateUserProfile(user, profile);
        return Created(profile);
    }

    [HttpPut("profiles({profileId})")]
    [EnableQuery]
    public async Task<IActionResult> UpdateUserProfile(Guid profileId, [FromBody] UserProfile profile)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUser();
        if (user == null) return Unauthorized();

        try
        {
            var profileDb = await _profilesOrchestrator.GetUserProfile(user, profileId);
            await _profilesOrchestrator.UpdateUserProfile(user, profile, profileDb);
            return Ok(profile);
        }
        catch (FrontendException e)
        {
            await _profilesOrchestrator.CreateUserProfile(user, profile);
            return Created(profile);
        }
    }

    [HttpPatch("profiles({profileId})")]
    [EnableQuery]
    public async Task<IActionResult> PartiallyUpdateUserProfile(Guid profileId, [FromBody] Delta<UserProfile> patch)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUser();

        try
        {
            var profileDb = await _profilesOrchestrator.GetUserProfile(user, profileId);
            var profile = await _profilesOrchestrator.PartiallyUpdateUserProfile(user, patch, profileDb);
            return Ok(profile);
        }
        catch (FrontendException e)
        {
            var profile = patch.GetInstance();
            await _profilesOrchestrator.CreateUserProfile(user, profile);
            return Created(profile);
        }
    }

    [HttpDelete("profiles({profileId})")]
    public async Task<IActionResult> DeleteUserProfile(Guid profileId)
    {
        var user = await GetCurrentUser();

        _profilesOrchestrator.DeleteUserProfile(user, profileId);
        return NoContent();
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    public virtual async Task<ApplicationUser> GetCurrentUser()
    {
        var username = HttpContext.User.Identity!.Name;
        var user = await _userManager.FindByNameAsync(username);
        return user;
    }
}