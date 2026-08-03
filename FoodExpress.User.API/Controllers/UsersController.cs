using FoodExpress.User.API.DTOs;
using FoodExpress.User.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodExpress.User.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Récupérer tous les utilisateurs (Admin uniquement)
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    /// <summary>
    /// Récupérer un utilisateur par ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>
    /// Récupérer les adresses d'un utilisateur
    /// </summary>
    [HttpGet("{id:guid}/addresses")]
    [Authorize]
    public async Task<IActionResult> GetAddresses(Guid id)
    {
        var addresses = await _userService.GetAddressesAsync(id);
        return Ok(addresses);
    }

    /// <summary>
    /// Ajouter une adresse à un utilisateur
    /// </summary>
    [HttpPost("{id:guid}/addresses")]
    [Authorize]
    public async Task<IActionResult> AddAddress(Guid id, [FromBody] CreateAddressDto dto)
    {
        try
        {
            var address = await _userService.AddAddressAsync(id, dto);
            return CreatedAtAction(nameof(GetAddresses), new { id }, address);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}