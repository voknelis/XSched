using Microsoft.AspNetCore.Identity;
using XSched.API.Dtos;
using XSched.API.Entities;
using XSched.API.Models;

namespace XSched.API.Orchestrators.Interfaces;

public interface IAuthenticateOrchestrator
{
    Task<IdentityResult> Register(RegisterModel model);

    Task<TokenResponse> Login(ApplicationUser user, ClientConnectionMetadata clientMeta);

    Task<TokenResponse> RefreshToken(RefreshTokenModel model, ClientConnectionMetadata clientMeta);
}