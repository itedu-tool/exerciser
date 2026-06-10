using System;

namespace Exerciser.WebApi.DTOs;

/// <summary>
/// DTO для краткой информации об экзамене (список экзаменов).
/// </summary>
public record ExamSummaryDto
{
    /// <summary>Уникальный идентификатор экзамена.</summary>
    public Guid Id { get; init; }

    /// <summary>Название экзамена.</summary>
    public required string Title { get; init; }

    /// <summary>Описание экзамена (необязательно).</summary>
    public string? Description { get; init; }

    /// <summary>Количество вопросов в экзамене.</summary>
    public int QuestionsCount { get; init; }

    /// <summary>Дата и время создания экзамена (UTC).</summary>
    public DateTime CreatedAt { get; init; }
}