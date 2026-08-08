using FoodExpress.Common.Auth;
using FoodExpress.Restaurant.API.DTOs;
using FoodExpress.Restaurant.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodExpress.Restaurant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DishesController : ControllerBase
{
    private readonly IDishService _service;

    public DishesController(IDishService service)
    {
        _service = service;
    }

    /// <summary>Récupérer les plats d'un restaurant</summary>
    [HttpGet("restaurant/{restaurantId:guid}")]
    public async Task<IActionResult> GetByRestaurant(Guid restaurantId)
    {
        var dishes = await _service.GetByRestaurantAsync(restaurantId);
        return Ok(dishes);
    }

    /// <summary>Récupérer un plat par ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dish = await _service.GetByIdAsync(id);
        return dish == null ? NotFound() : Ok(dish);
    }

    /// <summary>Créer un plat (dans SES restaurants uniquement)</summary>
    [HttpPost]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateDishDto dto)
    {
        try
        {
            var dish = await _service.CreateAsync(dto, CurrentOwnerId ?? Guid.Empty, IsAdmin);
            return CreatedAtAction(nameof(GetById), new { id = dish.Id }, dish);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // 403 : pas votre restaurant
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Modifier un plat (propriétaire du restaurant uniquement)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDishDto dto)
    {
        try
        {
            var dish = await _service.UpdateAsync(id, dto, CurrentOwnerId ?? Guid.Empty, IsAdmin);
            return dish == null ? NotFound() : Ok(dish);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // 403 : pas votre restaurant
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Supprimer un plat (propriétaire du restaurant uniquement)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, CurrentOwnerId ?? Guid.Empty, IsAdmin);
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // 403 : pas votre restaurant
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Uploader l'image d'un plat (propriétaire du restaurant uniquement)</summary>
    [HttpPost("{id:guid}/image")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Fichier vide");

        try
        {
            var url = await _service.UploadImageAsync(id, file, CurrentOwnerId ?? Guid.Empty, IsAdmin);
            return url == null ? NotFound() : Ok(new { imageUrl = url });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // 403 : pas votre restaurant
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid? CurrentOwnerId
    {
        get
        {
            var sub = User.FindFirst("sub")?.Value
                      ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    private bool IsAdmin => User.IsInRole(Roles.Admin);
}