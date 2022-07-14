using Microsoft.EntityFrameworkCore;
using XSched.API.DbContexts;
using XSched.API.Entities;
using XSched.API.Repositories.Interfaces;

namespace XSched.API.Repositories.Implementation;

public class ProfileRepository : IProfileRepository
{
    private readonly XSchedDbContext _dbContext;

    public ProfileRepository(XSchedDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public IQueryable<UserProfile> GetUserProfiles(string userId)
    {
        return _dbContext.Profiles.Where(p => p.UserId == userId);
    }

    public async Task<UserProfile?> GetUserProfileByIdAsync(string userId, Guid profileId)
    {
        return await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId && p.Id == profileId);
    }

    public void CreateProfile(UserProfile profile)
    {
        _dbContext.Profiles.Add(profile);
    }

    public virtual void UpdateProfile(UserProfile profileDb, UserProfile profile)
    {
        _dbContext.Entry(profileDb).CurrentValues.SetValues(profile);
    }

    public void DeleteProfile(UserProfile profile)
    {
        _dbContext.Profiles.Remove(profile);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
}