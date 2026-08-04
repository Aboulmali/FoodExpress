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

    /// <summary>Créer un plat</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateDishDto dto)
    {
        try
        {
            var dish = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = dish.Id }, dish);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Modifier un plat</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDishDto dto)
    {
        var dish = await _service.UpdateAsync(id, dto);
        return dish == null ? NotFound() : Ok(dish);
    }

    /// <summary>Supprimer un plat</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Uploader l'image d'un plat</summary>
    [HttpPost("{id:guid}/image")]
    [Authorize]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Fichier vide");

        var url = await _service.UploadImageAsync(id, file);
        return url == null ? NotFound() : Ok(new { imageUrl = url });
    }
}