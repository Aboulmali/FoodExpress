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

    // L'identité vient TOUJOURS du token JWT (claim "sub"), jamais du corps de la requête.
    private Guid? CurrentUserId
    {
        get
        {
            var sub = User.FindFirst("sub")?.Value
                      ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    private bool IsAdmin => User.IsInRole(Roles.Admin);

    /// <summary>Créer une nouvelle commande</summary>
    [HttpPost]
    [Authorize(Policy = Policies.CustomerOnly)]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var callerId = CurrentUserId;
        if (callerId == null)
            return Unauthorized(new { message = "Token invalide" });

        try
        {
            var order = await _service.CreateAsync(dto, callerId.Value);
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

    /// <summary>Récupérer une commande par ID (le client concerné ou un Admin)</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.CustomerOrAdmin)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var order = await _service.GetByIdAsync(id, CurrentUserId ?? Guid.Empty, IsAdmin);
            return order == null ? NotFound() : Ok(order);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // 403 : pas votre commande
        }
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

    /// <summary>Récupérer les commandes d'un restaurant (le propriétaire ou un Admin)</summary>
    [HttpGet("restaurant/{restaurantId:guid}")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> GetByRestaurant(Guid restaurantId)
    {
        try
        {
            var orders = await _service.GetByRestaurantAsync(restaurantId, CurrentUserId ?? Guid.Empty, IsAdmin);
            return Ok(orders);
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

    /// <summary>Récupérer les commandes d'un livreur (l'agent concerné ou un Admin)</summary>
    [HttpGet("delivery/{deliveryPersonId:guid}")]
    [Authorize(Policy = Policies.DeliveryOrAdmin)]
    public async Task<IActionResult> GetByDeliveryPerson(Guid deliveryPersonId)
    {
        // Un livreur ne voit QUE les commandes qui lui sont assignées.
        if (!User.IsInRole("Admin"))
        {
            var sub = User.FindFirst("sub")?.Value
                      ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(sub, out var currentId) || currentId != deliveryPersonId)
                return Forbid(); // 403 : ce ne sont pas vos livraisons
        }

        var orders = await _service.GetByDeliveryPersonAsync(deliveryPersonId);
        return Ok(orders);
    }

    /// <summary>Mettre à jour le statut de livraison d'une commande (livreur assigné ou Admin)</summary>
    [HttpPut("{id:guid}/delivery-status")]
    [Authorize(Policy = Policies.DeliveryOrAdmin)]
    public async Task<IActionResult> UpdateDeliveryStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        var sub = User.FindFirst("sub")?.Value
                  ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid.TryParse(sub, out var callerId);
        var isAdmin = User.IsInRole("Admin");

        try
        {
            var order = await _service.UpdateDeliveryStatusAsync(id, dto, callerId, isAdmin);
            return order == null ? NotFound() : Ok(order);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Mettre à jour le statut d'une commande (le propriétaire du restaurant ou un Admin)</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        try
        {
            var order = await _service.UpdateStatusAsync(id, dto, CurrentUserId ?? Guid.Empty, IsAdmin);
            return order == null ? NotFound() : Ok(order);
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

    /// <summary>Assigner un livreur à une commande</summary>
    [HttpPost("{id:guid}/assign-delivery")]
    [Authorize(Policy = Policies.RestaurantAdmin)]
    public async Task<IActionResult> AssignDelivery(Guid id, [FromBody] AssignDeliveryDto dto)
    {
        var order = await _service.AssignDeliveryAsync(id, dto);
        return order == null ? NotFound() : Ok(order);
    }

    /// <summary>Annuler une commande (le client concerné ou un Admin)</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.CustomerOrAdmin)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string reason)
    {
        try
        {
            var success = await _service.CancelAsync(id, reason, CurrentUserId ?? Guid.Empty, IsAdmin);
            return success ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // 403 : pas votre commande
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
