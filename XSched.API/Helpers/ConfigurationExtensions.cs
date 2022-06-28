namespace XSched.API.Helpers;

public static class ConfigurationExtensions
{
    public static string GetJWTString(this IConfiguration configuration, string name)
    {
        return configuration?.GetSection("JWT")?[name];
    }
}