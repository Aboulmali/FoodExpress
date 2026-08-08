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
        var restaurant = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = restaurant.Id }, restaurant);
    }

    /// <summary>Mettre à jour un restaurant</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRestaurantDto dto)
    {
        var restaurant = await _service.UpdateAsync(id, dto);
        return restaurant == null ? NotFound() : Ok(restaurant);
    }

    /// <summary>Supprimer un restaurant</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
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