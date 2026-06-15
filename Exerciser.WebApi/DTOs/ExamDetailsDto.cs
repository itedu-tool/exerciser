using System;
using System.Collections.Generic;

namespace Exerciser.WebApi.DTOs;

/// <summary>
/// DTO для полной информации об экзамене (включая вопросы и правильные ответы).
/// </summary>
public record ExamDetailsDto
{
    /// <summary>Уникальный идентификатор экзамена.</summary>
    public Guid Id { get; init; }

    /// <summary>Название экзамена.</summary>
    public required string Title { get; init; }

    /// <summary>Описание экзамена (необязательно).</summary>
    public string? Description { get; init; }

    /// <summary>Дата и время создания экзамена (UTC).</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Список вопросов экзамена.</summary>
    public List<QuestionDetailsDto> Questions { get; init; } = [];

    /// <summary>Количество вопросов SingleChoice для показа.</summary>
    public int SingleChoiceToShow { get; init; }

    /// <summary>Количество вопросов MultipleChoice для показа.</summary>
    public int MultipleChoiceToShow { get; init; }

    /// <summary>Количество вопросов TextInput для показа.</summary>
    public int TextInputToShow { get; init; }
}