using Microsoft.AspNetCore.Identity;

namespace XSched.API.Entities;

public class ApplicationUser: IdentityUser
{
    public string? RefreshToken { get; set; }

    public DateTime RefreshTokenExpiry { get; set; }
}