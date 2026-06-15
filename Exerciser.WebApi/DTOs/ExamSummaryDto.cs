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

    /// <summary>Количество вопросов типа SingleChoice.</summary>
    public int SingleChoiceCount { get; init; }

    /// <summary>Количество вопросов типа MultipleChoice.</summary>
    public int MultipleChoiceCount { get; init; }

    /// <summary>Количество вопросов типа TextInput.</summary>
    public int TextInputCount { get; init; }

    /// <summary>Количество вопросов SingleChoice для показа.</summary>
    public int SingleChoiceToShow { get; init; }

    /// <summary>Количество вопросов MultipleChoice для показа.</summary>
    public int MultipleChoiceToShow { get; init; }

    /// <summary>Количество вопросов TextInput для показа.</summary>
    public int TextInputToShow { get; init; }

    /// <summary>Дата и время создания экзамена (UTC).</summary>
    public DateTime CreatedAt { get; init; }
}