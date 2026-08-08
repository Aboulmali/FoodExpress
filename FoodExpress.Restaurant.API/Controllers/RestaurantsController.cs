using FoodExpress.Common.Auth;
using FoodExpress.Restaurant.API.DTOs;
using FoodExpress.Restaurant.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodExpress.Restaurant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantService _service;

    public RestaurantsController(IRestaurantService service)
    {
        _service = service;
    }

    // L'identité du propriétaire vient TOUJOURS du token JWT (claim "sub"),
    // jamais du corps de la requête — interdit l'usurpation d'identité.
    private Guid? CurrentOwnerId
    {
        get
        {
            var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    /// <summary>Récupérer tous les restaurants (avec cache Redis)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var restaurants = await _service.GetAllAsync();
        return Ok(restaurants);
    }

    /// <summary>Récupérer un restaurant par ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var restaurant = await _service.GetByIdAsync(id);
        return restaurant == null ? NotFound() : Ok(restaurant);
    }

    /// <summary>Créer un restaurant (RestaurantOwner ou Admin)</summary>
    [HttpPost]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateRestaurantDto dto)
    {
        var ownerId = CurrentOwnerId;
        if (ownerId == null)
            return Unauthorized(new { message = "Token invalide" });

        var restaurant = await _service.CreateAsync(dto, ownerId.Value);
        return CreatedAtAction(nameof(GetById), new { id = restaurant.Id }, restaurant);
    }

    /// <summary>Mettre à jour un restaurant (propriétaire uniquement)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRestaurantDto dto)
    {
        var ownerId = CurrentOwnerId;
        if (ownerId == null)
            return Unauthorized(new { message = "Token invalide" });

        try
        {
            var restaurant = await _service.UpdateAsync(id, dto, ownerId.Value);
            return restaurant == null ? NotFound() : Ok(restaurant);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(); // 403 : pas votre restaurant
        }
    }

    /// <summary>Supprimer un restaurant (propriétaire uniquement)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ownerId = CurrentOwnerId;
        if (ownerId == null)
            return Unauthorized(new { message = "Token invalide" });

        try
        {
            var deleted = await _service.DeleteAsync(id, ownerId.Value);
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // 403 : pas votre restaurant
        }
    }

    /// <summary>Uploader le logo d'un restaurant</summary>
    [HttpPost("{id:guid}/logo")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> UploadLogo(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Fichier vide");

        var url = await _service.UploadLogoAsync(id, file);
        return url == null ? NotFound() : Ok(new { logoUrl = url });
    }
}