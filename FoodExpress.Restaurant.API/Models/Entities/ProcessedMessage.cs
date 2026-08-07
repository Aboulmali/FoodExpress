namespace FoodExpress.Restaurant.API.Models.Entities;

/// <summary>
/// Journal des événements déjà traités (idempotence des consumers).
/// </summary>
public class ProcessedMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}