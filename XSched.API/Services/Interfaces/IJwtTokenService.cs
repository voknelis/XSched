using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using XSched.API.Dtos;

namespace XSched.API.Services.Interfaces;

public interface IJwtTokenService
{
    AccessTokenResult GenerateAccessToken(IEnumerable<Claim> claims);
    RefreshTokenResult GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}