using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodExpress.Common.HealthChecks;

/// <summary>
/// Écrit un JSON lisible pour l'endpoint de santé :
/// { status, totalDuration, checks: [ { name, status, description, duration } ] }
/// </summary>
public static class HealthCheckJsonWriter
{
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        using var stream = new MemoryStream();
        await using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("status", report.Status.ToString());
            writer.WriteString("totalDuration", report.TotalDuration.ToString());

            writer.WriteStartArray("checks");
            foreach (var entry in report.Entries)
            {
                writer.WriteStartObject();
                writer.WriteString("name", entry.Key);
                writer.WriteString("status", entry.Value.Status.ToString());
                writer.WriteString("description", entry.Value.Description ?? string.Empty);
                writer.WriteString("duration", entry.Value.Duration.ToString());
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
            await writer.FlushAsync();
        }

        stream.Position = 0;
        await stream.CopyToAsync(context.Response.Body);
    }
}