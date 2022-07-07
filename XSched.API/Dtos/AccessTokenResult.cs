using System.IdentityModel.Tokens.Jwt;

namespace XSched.API.Dtos;

public class AccessTokenResult
{
    public JwtSecurityToken Token { get; set; }
    public string TokenString { get; set; }
}