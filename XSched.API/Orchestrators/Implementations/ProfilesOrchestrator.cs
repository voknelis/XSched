using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;
using XSched.API.Entities;
using XSched.API.Models;
using XSched.API.Orchestrators.Interfaces;
using XSched.API.Repositories.Interfaces;

namespace XSched.API.Orchestrators.Implementations;

public class ProfilesOrchestrator : IProfilesOrchestrator
{
    private readonly IProfileRepository _profileRepository;

    public ProfilesOrchestrator(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<IEnumerable<UserProfile>> GetUserProfiles(ApplicationUser user)
    {
        return await _profileRepository.GetUserProfiles(user.Id).ToListAsync();
    }

    public async Task<UserProfile> GetUserProfile(ApplicationUser user, Guid profileId)
    {
        var profile = await _profileRepository.GetUserProfileByIdAsync(user.Id, profileId);
        if (profile == null)
            throw new FrontendException($"Requested profile was not found", StatusCodes.Status404NotFound);
        return profile;
    }

    public virtual async Task CreateUserProfile(ApplicationUser user, UserProfile profile)
    {
        profile.UserId = user.Id;

        if (profile.IsDefault) await ResetDefaultUserProfileState(user);

        _profileRepository.CreateProfile(profile);
        await _profileRepository.SaveChangesAsync();
    }

    public virtual async Task UpdateUserProfile(ApplicationUser user, UserProfile profile, UserProfile profileDb)
    {
        profile.Id = profileDb.Id;
        profile.UserId = profileDb.UserId;

        if (profile.IsDefault) await ResetDefaultUserProfileState(user);

        _profileRepository.UpdateProfile(profileDb, profile);
        await _profileRepository.SaveChangesAsync();
    }

    public virtual async Task<UserProfile> PartiallyUpdateUserProfile(ApplicationUser user, Delta<UserProfile> patch,
        UserProfile profileDb)
    {
        var profile = patch.GetInstance();
        if (profile.IsDefault) await ResetDefaultUserProfileState(user);

        patch.Patch(profileDb);
        await _profileRepository.SaveChangesAsync();

        return profileDb;
    }

    public async Task DeleteUserProfile(ApplicationUser user, Guid profileId)
    {
        var profile = await _profileRepository.GetUserProfileByIdAsync(user.Id, profileId);

        if (profile == null)
            throw new FrontendException($"Requested profile was not found", StatusCodes.Status404NotFound);

        if (profile.IsDefault) throw new FrontendException("Default profile cannot be deleted.");

        _profileRepository.DeleteProfile(profile);
        await _profileRepository.SaveChangesAsync();
    }

    private async Task ResetDefaultUserProfileState(ApplicationUser user)
    {
        var profiles = await _profileRepository.GetUserProfiles(user.Id).ToListAsync();
        foreach (var profile in profiles) profile.IsDefault = false;
    }
}