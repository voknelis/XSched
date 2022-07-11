using Microsoft.AspNetCore.OData.Deltas;
using XSched.API.Entities;

namespace XSched.API.Orchestrators.Interfaces;

public interface IProfilesOrchestrator
{
    public Task<IEnumerable<UserProfile>> GetUserProfiles(ApplicationUser user);
    public Task<UserProfile> GetUserProfile(ApplicationUser user, Guid profileId);
    public Task CreateUserProfile(ApplicationUser user, UserProfile profile);
    public Task UpdateUserProfile(ApplicationUser user, UserProfile profile, Guid profileId);
    public Task<UserProfile> PartiallyUpdateUserProfile(ApplicationUser user, Delta<UserProfile> patch, Guid profileId);
    public void DeleteUserProfile(ApplicationUser user, Guid profileId);
}