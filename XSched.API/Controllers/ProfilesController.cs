using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using XSched.API.Entities;
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
        _userManager = userManager;
        _profilesOrchestrator = profilesOrchestrator;
    }

    [HttpGet("profiles")]
    [EnableQuery]
    public async Task<IActionResult> GetUserProfiles()
    {
        var user = await GetCurrentUser();
        if (user == null) return Unauthorized();

        return Ok(await _profilesOrchestrator.GetUserProfiles(user));
    }

    [HttpGet("profiles({profileId})")]
    [EnableQuery]
    public async Task<IActionResult> GetUserProfile(Guid profileId)
    {
        var user = await GetCurrentUser();
        if (user == null) return Unauthorized();

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
    public async Task<IActionResult> CreateUserProfile(Guid profileId, [FromBody] UserProfile profile)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUser();
        if (user == null) return Unauthorized();

        await _profilesOrchestrator.UpdateUserProfile(user, profile, profileId);
        return Ok(profile);
    }

    [HttpPatch("profiles({profileId})")]
    [EnableQuery]
    public async Task<IActionResult> PartiallyUpdateUserProfile(Guid profileId, [FromBody] Delta<UserProfile> patch)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUser();
        if (user == null) return Unauthorized();

        var updatedProfile = _profilesOrchestrator.PartiallyUpdateUserProfile(user, patch, profileId);
        return Ok(updatedProfile);
    }

    [HttpDelete("profiles({profileId})")]
    public async Task<IActionResult> DeleteUserProfile(Guid profileId)
    {
        var username = HttpContext.User.Identity.Name;

        var user = await GetCurrentUser();
        if (user == null) return Unauthorized();

        _profilesOrchestrator.DeleteUserProfile(user, profileId);
        return NoContent();
    }

    private async Task<ApplicationUser?> GetCurrentUser()
    {
        var username = HttpContext.User.Identity.Name;
        var user = await _userManager.FindByNameAsync(username);
        return user;
    }
}