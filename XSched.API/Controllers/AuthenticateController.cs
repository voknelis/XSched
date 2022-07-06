﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using XSched.API.Entities;
using XSched.API.Models;
using XSched.API.Orchestrators.Interfaces;
using XSched.API.Services.Interfaces;

namespace XSched.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticateController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthenticateOrchestrator _authenticateOrchestrator;

    public AuthenticateController(
        UserManager<ApplicationUser> userManager,
        IAuthenticateOrchestrator authenticateOrchestrator)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _authenticateOrchestrator = authenticateOrchestrator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var userExists = await _userManager.FindByNameAsync(model.Username);
        if (userExists != null) return BadRequest("User already exist");

        var result = await _authenticateOrchestrator.Register(model);
        if (!result.Succeeded) return StatusCode(StatusCodes.Status500InternalServerError, result.Errors);

        return NoContent();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null) return BadRequest("Client not found");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!isPasswordValid) return BadRequest("Invalid login or password");

        var tokenResponse = await _authenticateOrchestrator.Login(user);
        return Ok(tokenResponse);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model)
    {
        if (model == null) return BadRequest("Access and refresh token should be specified");
        var tokenResponse = await _authenticateOrchestrator.RefreshToken(model);
        return Ok(tokenResponse);
    }
}