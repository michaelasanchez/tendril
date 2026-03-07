using Auth.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tendril.Core.Domain.Dtos;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController(ITokenService _tokenService, IUserRepository _userRepository) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] string code)
    {
        var tokenResponse = await _tokenService.ExchangeTokenAsync(code);

        var payload = await _tokenService.ValidateTokenAsync(tokenResponse);

        var user = await _userRepository.UpsertUserAsync(new UserDto
        {
            GoogleSub = payload.Subject,
            Email = payload.Email,
            Name = payload.Name,
            PictureUrl = payload.Picture,
            RefreshToken = tokenResponse.RefreshToken
        });

        // 1. Create the identity (This is what populates User.FindFirst in other endpoints)
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.GoogleSub),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // 2. Issue the cookie
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties { IsPersistent = true }
        );

        return Ok(new { user.Name, user.PictureUrl, user.Email });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // This clears the 'Tendril.Auth' cookie from the browser
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Ok(new { Message = "Logged out successfully" });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        // 1. Get the 'sub' (Google ID) from the current logged-in user claims
        var googleSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(googleSub)) return Unauthorized();

        // 2. Fetch the user from your DB
        var user = await _userRepository.GetUserByGoogleSubAsync(googleSub);

        if (user == null) return NotFound();

        // 3. Return the profile info React needs to display the UI
        return Ok(new { user.Name, user.PictureUrl, user.Email });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var googleSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(googleSub)) return Unauthorized();

        var user = await _userRepository.GetUserByGoogleSubAsync(googleSub);

        // 4. Use the RefreshToken stored in your DB
        if (string.IsNullOrEmpty(user?.RefreshToken)) return BadRequest("No refresh token available.");

        try
        {
            var tokenResponse = await _tokenService.RefreshTokenAsync(user.RefreshToken);

            // 5. Google might send a NEW refresh token; if so, update the DB
            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                await _userRepository.UpdateRefreshTokenAsync(user.Id, tokenResponse.RefreshToken);
            }

            return Ok(new { Message = "Token refreshed successfully" });
        }
        catch (Exception)
        {
            return Unauthorized("Session expired, please login again.");
        }
    }
}
