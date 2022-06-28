using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace XSched.API.Services.Interfaces;

public interface IJwtTokenService
{
    JwtSecurityToken GenerateAccessToken(IEnumerable<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}