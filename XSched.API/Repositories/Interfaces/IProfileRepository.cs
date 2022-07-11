using XSched.API.Entities;

namespace XSched.API.Repositories.Interfaces;

public interface IProfileRepository
{
    public IQueryable<UserProfile> GetUserProfiles(string userId);
    public Task<UserProfile?> GetUserProfileById(string userId, Guid profileId);
    public void CreateProfile(UserProfile profile);
    public void UpdateProfile(UserProfile profileDb, UserProfile profile);
    public void DeleteProfile(UserProfile profile);
    public Task<int> SaveChangesAsync();
}