using FoodExpress.Common.Auth;
using FoodExpress.Order.API.DTOs;
using FoodExpress.Order.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodExpress.Order.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service)
    {
        _service = service;
    }

    /// <summary>Créer une nouvelle commande</summary>
    [HttpPost]
    [Authorize(Policy = Policies.CustomerOnly)]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        try
        {
            var order = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Récupérer une commande par ID</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.CustomerOrAdmin)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _service.GetByIdAsync(id);
        return order == null ? NotFound() : Ok(order);
    }

    /// <summary>Récupérer toutes les commandes</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _service.GetAllAsync();
        return Ok(orders);
    }

    /// <summary>Récupérer les commandes d'un client (le client concerné ou un Admin)</summary>
    [HttpGet("customer/{customerId:guid}")]
    [Authorize(Policy = Policies.CustomerOrAdmin)]
    public async Task<IActionResult> GetByCustomer(Guid customerId)
    {
        // Un client ne peut consulter QUE ses propres commandes (claim "sub" du JWT).
        if (!User.IsInRole("Admin"))
        {
            var sub = User.FindFirst("sub")?.Value
                      ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(sub, out var currentId) || currentId != customerId)
                return Forbid(); // 403 : ce ne sont pas vos commandes
        }

        var orders = await _service.GetByCustomerAsync(customerId);
        return Ok(orders);
    }

    /// <summary>Récupérer les commandes d'un restaurant</summary>
    [HttpGet("restaurant/{restaurantId:guid}")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> GetByRestaurant(Guid restaurantId)
    {
        var orders = await _service.GetByRestaurantAsync(restaurantId);
        return Ok(orders);
    }

    /// <summary>Mettre à jour le statut d'une commande</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        var order = await _service.UpdateStatusAsync(id, dto);
        return order == null ? NotFound() : Ok(order);
    }

    /// <summary>Assigner un livreur à une commande</summary>
    [HttpPost("{id:guid}/assign-delivery")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> AssignDelivery(Guid id, [FromBody] AssignDeliveryDto dto)
    {
        var order = await _service.AssignDeliveryAsync(id, dto);
        return order == null ? NotFound() : Ok(order);
    }

    /// <summary>Annuler une commande</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.CustomerOrAdmin)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string reason)
    {
        try
        {
            var success = await _service.CancelAsync(id, reason);
            return success ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
