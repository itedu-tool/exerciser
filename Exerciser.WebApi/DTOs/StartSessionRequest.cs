using System;

namespace Exerciser.WebApi.DTOs;

/// <summary>Запрос на создание сессии (логин студента).</summary>
public record StartSessionRequest
{
    /// <summary>Идентификатор группы.</summary>
    public required Guid GroupId { get; init; }
    
    /// <summary>Идентификатор студента.</summary>
    public required Guid StudentId { get; init; }
}