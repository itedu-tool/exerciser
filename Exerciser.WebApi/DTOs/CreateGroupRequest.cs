namespace Exerciser.WebApi.DTOs;

/// <summary>Запрос на создание новой группы.</summary>
public record CreateGroupRequest
{
    /// <summary>Название группы.</summary>
    public required string Name { get; init; }
}