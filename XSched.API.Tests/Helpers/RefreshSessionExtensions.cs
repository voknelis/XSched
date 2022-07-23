using XSched.API.Entities;

namespace XSched.API.Tests.Helpers;

public static class RefreshSessionExtensions
{
    public static RefreshSession Clone(this RefreshSession refreshSession)
    {
        return new RefreshSession()
        {
            Id = refreshSession.Id,
            UserId = refreshSession.UserId,
            User = refreshSession.User,
            RefreshToken = refreshSession.RefreshToken,
            ExpiresIn = refreshSession.ExpiresIn,
            Created = refreshSession.Created,
            Fingerprint = refreshSession.Fingerprint,
            UserAgent = refreshSession.UserAgent,
            Ip = refreshSession.Ip
        };
    }
}