using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using XSched.API.DbContexts;
using XSched.API.Entities;
using XSched.API.Models;

namespace XSched.API.Controllers;

[Authorize]
[Route("odata")]
public class ProfilesController : ODataController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly XSchedDbContext _dbContext;

    public ProfilesController(UserManager<ApplicationUser> userManager, XSchedDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    [HttpGet("profiles")]
    [EnableQuery]
    public async Task<IActionResult> GetUserProfiles()
    {
        var username = HttpContext.User.Identity.Name;

        var user = await _userManager.FindByNameAsync(username);
        if (user is null) return NotFound();

        var profiles = await _dbContext.Profiles.Where(p => p.UserId == user.Id).ToListAsync();
        return Ok(profiles);
    }

    [HttpGet("profiles({profileId})")]
    [EnableQuery]
    public async Task<IActionResult> GetUserProfile(Guid profileId)
    {
        var username = HttpContext.User.Identity.Name;

        var user = await _userManager.FindByNameAsync(username);
        if (user is null) return NotFound();

        var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Id == profileId);
        if (profile is null) return NotFound();
        return Ok(profile);
    }

    [HttpPost("profiles")]
    [EnableQuery]
    public async Task<IActionResult> CreateUserProfile([FromBody] UserProfile profile)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var username = HttpContext.User.Identity.Name;

        var user = await _userManager.FindByNameAsync(username);
        if (user is null) return NotFound();

        profile.UserId = user.Id;
        _dbContext.Profiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        return Created(profile);
    }

    [HttpPut("profiles({profileId})")]
    [EnableQuery]
    public async Task<IActionResult> CreateUserProfile(Guid profileId, [FromBody] UserProfile profile)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var username = HttpContext.User.Identity.Name;

        var user = await _userManager.FindByNameAsync(username);
        if (user is null) return NotFound();

        var profileDb = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.Id == profileId && p.UserId == user.Id);
        if (profileDb is null)
        {
            profile.Id = profileId;
            profile.UserId = user.Id;
            _dbContext.Profiles.Add(profile);
            await _dbContext.SaveChangesAsync();

            return Created(profile);
        }

        profile.UserId = profileDb.UserId;
        _dbContext.Entry(profileDb).CurrentValues.SetValues(profile);
        await _dbContext.SaveChangesAsync();

        return Ok(profile);
    }

    [HttpPatch("profiles({profileId})")]
    [EnableQuery]
    public async Task<IActionResult> PartiallyUpdateUserProfile(Guid profileId, [FromBody] Delta<UserProfile> patch)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var username = HttpContext.User.Identity.Name;

        var user = await _userManager.FindByNameAsync(username);
        if (user is null) return NotFound();

        var profile = patch.GetInstance();
        var profileDb = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.Id == profileId && p.UserId == user.Id);
        if (profileDb is null)
        {
            profile.Id = profileId;
            profile.UserId = user.Id;
            _dbContext.Profiles.Add(profile);
            await _dbContext.SaveChangesAsync();
            return Created(profile);
        }

        patch.Patch(profileDb);
        await _dbContext.SaveChangesAsync();

        return Ok(profileDb);
    }

    [HttpDelete("profiles({profileId})")]
    public async Task<IActionResult> DeleteUserProfile(Guid profileId)
    {
        var username = HttpContext.User.Identity.Name;

        var user = await _userManager.FindByNameAsync(username);
        if (user is null) return NotFound();

        var profileDb = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.Id == profileId && p.UserId == user.Id);
        if (profileDb == null) return NotFound();

        _dbContext.Profiles.Remove(profileDb);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}