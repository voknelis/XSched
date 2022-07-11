﻿using Microsoft.AspNetCore.OData.Deltas;
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
        var profile = await _profileRepository.GetUserProfileById(user.Id, profileId);
        if (profile == null)
            throw new FrontendException($"Requested profile was not found", StatusCodes.Status404NotFound);
        return profile;
    }

    public async Task CreateUserProfile(ApplicationUser user, UserProfile profile)
    {
        profile.UserId = user.Id;
        _profileRepository.CreateProfile(profile);
        await _profileRepository.SaveChangesAsync();
    }

    public async Task UpdateUserProfile(ApplicationUser user, UserProfile profile, Guid profileId)
    {
        var profileDb = await _profileRepository.GetUserProfileById(user.Id, profileId);
        if (profileDb == null)
        {
            // upserting
            profile.Id = profileId;
            await CreateUserProfile(user, profile);
            return;
        }

        profile.Id = profileDb.Id;
        _profileRepository.UpdateProfile(profileDb, profile);
        await _profileRepository.SaveChangesAsync();
    }

    public async Task<UserProfile> PartiallyUpdateUserProfile(ApplicationUser user, Delta<UserProfile> patch,
        Guid profileId)
    {
        var profileDb = await _profileRepository.GetUserProfileById(user.Id, profileId);
        if (profileDb == null)
        {
            // upserting
            var profile = patch.GetInstance();

            profile.Id = profileId;
            await CreateUserProfile(user, profile);
            return profile;
        }

        patch.Patch(profileDb);
        await _profileRepository.SaveChangesAsync();

        return profileDb;
    }

    public async void DeleteUserProfile(ApplicationUser user, Guid profileId)
    {
        var profile = await _profileRepository.GetUserProfileById(user.Id, profileId);
        if (profile == null)
            throw new FrontendException($"Requested profile was not found", StatusCodes.Status404NotFound);
        _profileRepository.DeleteProfile(profile);
        await _profileRepository.SaveChangesAsync();
    }
}