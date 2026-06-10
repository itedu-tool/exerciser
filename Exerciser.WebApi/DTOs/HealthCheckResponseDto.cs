using System;
using System.Text.Json.Serialization;

namespace Exerciser.WebApi.DTOs;

/// <summary>DTO ответа для health check.</summary>
public record HealthCheckResponseDto
{
    /// <summary>Статус приложения.</summary>
    public required string Status { get; set; }

    /// <summary>Локальное время.</summary>
    public required DateTime Timestamp { get; set; }

    /// <summary>UTC время.</summary>
    public required DateTime TimestampUtc { get; set; }

    /// <summary>Временная зона.</summary>
    public required string TimeZone { get; set; }

    /// <summary>Смещение временной зоны от UTC.</summary>
    public required string Offset { get; set; }

    /// <summary>Версия API (опционально).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ApiVersion { get; set; }
}