using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using XSched.API.Dtos;
using XSched.API.Helpers;
using XSched.API.Services.Interfaces;

namespace XSched.API.Services.Implementations;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public AccessTokenResult GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetJwtSecret()));
        var tokenExpiresIn = DateTime.Now.AddHours(_configuration.GetJwtAccessTokenValidity());
        var token = new JwtSecurityToken(
            _configuration.GetJwtIssuer(),
            _configuration.GetJwtAudience(),
            expires: tokenExpiresIn,
            claims: claims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));
        return new AccessTokenResult
        {
            Token = token,
            TokenString = new JwtSecurityTokenHandler().WriteToken(token)
        };
    }

    public RefreshTokenResult GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return new RefreshTokenResult
        {
            Token = Convert.ToBase64String(randomNumber),
            ExpiresIn = DateTime.Now.AddDays(_configuration.GetJwtRefreshTokenValidity())
        };
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters()
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetJwtSecret())),
            ValidateLifetime = false
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase)) throw new SecurityTokenException();
        return principal;
    }
}