using System.Text;

namespace XSched.API.Helpers;

public static class ConfigurationExtensions
{
    public static string GetJwtString(this IConfiguration configuration, string name)
    {
        return configuration?.GetSection("JWT")?[name];
    }

    public static string GetJwtSecret(this IConfiguration configuration)
    {
        return configuration?.GetJwtString("Secret");
    }

    public static string GetJwtIssuer(this IConfiguration configuration)
    {
        return configuration?.GetJwtString("ValidIssuer");
    }

    public static string GetJwtAudience(this IConfiguration configuration)
    {
        return configuration?.GetJwtString("ValidAudience");
    }

    /// <summary>
    /// Returns number of minutes, which refresh token remains valid
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static int GetJwtAccessTokenValidity(this IConfiguration configuration)
    {
        var accessTokenString = configuration?.GetJwtString("TokenValidityInHours");
        int.TryParse(accessTokenString, out var accessTokenValidityInHours);
        return accessTokenValidityInHours;
    }

    /// <summary>
    /// Returns number of days, which refresh token remains valid
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static int GetJwtRefreshTokenValidity(this IConfiguration configuration)
    {
        var refreshTokenString = configuration?.GetJwtString("RefreshTokenValidityInDays");
        int.TryParse(refreshTokenString, out var refreshTokenValidityInDays);
        return refreshTokenValidityInDays;
    }
}