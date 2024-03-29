﻿using XSched.API.Entities;

namespace XSched.API.Repositories.Interfaces;

public interface IProfileRepository
{
    public IQueryable<UserProfile> GetUserProfiles(string userId);
    public Task<UserProfile?> GetUserProfileByIdAsync(string userId, Guid profileId);
    public Task<UserProfile?> GetDefaultUserProfileAsync(string userId);
    public void CreateProfile(UserProfile profile);
    public void UpdateProfile(UserProfile profileDb, UserProfile profile);
    public void DeleteProfile(UserProfile profile);
    public Task<int> SaveChangesAsync();
}