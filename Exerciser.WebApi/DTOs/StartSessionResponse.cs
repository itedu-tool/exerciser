using System;

namespace Exerciser.WebApi.DTOs;

/// <summary>Ответ при создании сессии.</summary>
public record StartSessionResponse
{
    /// <summary>Идентификатор сессии.</summary>
    public Guid SessionId { get; init; }
}