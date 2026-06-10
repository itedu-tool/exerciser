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
}

/// <summary>
/// DTO для вопроса в составе детального ответа об экзамене.
/// </summary>
public record QuestionDetailsDto
{
    /// <summary>Уникальный идентификатор вопроса.</summary>
    public Guid Id { get; init; }

    /// <summary>Текст вопроса.</summary>
    public required string Text { get; init; }

    /// <summary>Тип вопроса: SingleChoice, MultipleChoice, TextInput.</summary>
    public required string Type { get; init; }

    /// <summary>Варианты ответов (для SingleChoice и MultipleChoice).</summary>
    public List<string> Options { get; init; } = [];

    /// <summary>Правильные ответы (строки).</summary>
    public List<string> CorrectAnswers { get; init; } = [];
}