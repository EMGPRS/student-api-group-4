using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistration.Api.Authorization;

namespace StudentRegistration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var role = (request.Username, request.Password) switch
        {
            ("viewer", "viewer123") => AppRoles.Viewer,
            ("admin", "admin123") => AppRoles.Admin,
            _ => null
        };

        if (role is null)
        {
            return Unauthorized();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, role)
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        return Ok(new LoginResponse(request.Username, role));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<LoginResponse> Me()
    {
        var username = User.Identity?.Name;
        var role = User.FindFirstValue(ClaimTypes.Role);

        return username is null || role is null
            ? Unauthorized()
            : Ok(new LoginResponse(username, role));
    }
}

/// <summary>Credentials used to start an authenticated API session.</summary>
public sealed record LoginRequest(string Username, string Password);

/// <summary>Describes the authenticated demonstration user.</summary>
public sealed record LoginResponse(string Username, string Role);
