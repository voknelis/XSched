using XSched.API.Entities;

namespace XSched.API.Tests.Helpers;

public static class UserProfileExtensions
{
    public static UserProfile Clone(this UserProfile profile)
    {
        return new UserProfile()
        {
            Id = profile.Id,
            Title = profile.Title,
            User = profile.User,
            UserId = profile.UserId
        };
    }
}