using FoodExpress.Common.Auth;
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
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    /// <summary>
    /// Récupérer un utilisateur par ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>
    /// Liste des livreurs (restaurateurs et admins : assignation de livraison)
    /// </summary>
    [HttpGet("delivery")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> GetDeliveryPersons()
    {
        var couriers = await _userService.GetDeliveryPersonsAsync();
        return Ok(couriers);
    }

    /// <summary>
    /// Changer le rôle d'un utilisateur (Admin uniquement ; synchronisé avec Keycloak)
    /// </summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateUserRoleDto dto)
    {
        try
        {
            var user = await _userService.UpdateRoleAsync(id, dto.Role);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Récupérer les adresses d'un utilisateur
    /// </summary>
    [HttpGet("{id:guid}/addresses")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetAddresses(Guid id)
    {
        var addresses = await _userService.GetAddressesAsync(id);
        return Ok(addresses);
    }

    /// <summary>
    /// Ajouter une adresse à un utilisateur
    /// </summary>
    [HttpPost("{id:guid}/addresses")]
    [Authorize(Policy = Policies.AdminOnly)]
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