using FoodExpress.User.API.DTOs;
using FoodExpress.User.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodExpress.User.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Inscription d'un nouvel utilisateur
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        try
        {
            var user = await _userService.RegisterAsync(dto);
            return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Connexion (récupère un token JWT)
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var token = await _userService.LoginAsync(dto);
            return Ok(token);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Email ou mot de passe incorrect" });
        }
    }

    /// <summary>
    /// Renouvellement du token (refresh token rotation)
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest(new { message = "Refresh token requis" });

        try
        {
            var token = await _userService.RefreshAsync(dto.RefreshToken);
            return Ok(token);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Session expirée. Veuillez vous reconnecter." });
        }
    }

    /// <summary>
    /// Déconnexion : révoque le refresh token côté Keycloak
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.RefreshToken))
            await _userService.LogoutAsync(dto.RefreshToken);
        return NoContent();
    }
}