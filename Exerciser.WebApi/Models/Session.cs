using System;

namespace Exerciser.WebApi.Models;

/// <summary>Сессия входа студента (без пароля).</summary>
public record Session
{
    /// <summary>Уникальный идентификатор сессии.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Снимок данных студента на момент входа.</summary>
    public required StudentSnapshot Student { get; set; }

    /// <summary>Время создания сессии (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}