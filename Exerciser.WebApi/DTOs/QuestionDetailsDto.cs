using System;
using System.Collections.Generic;

using Exerciser.WebApi.Models;

namespace Exerciser.WebApi.DTOs;

/// <summary>DTO для детального отображения вопроса (с правильными ответами).</summary>
public record QuestionDetailsDto
{
    /// <summary>Идентификатор вопроса.</summary>
    public Guid Id { get; init; }

    /// <summary>Текст вопроса.</summary>
    public required string Text { get; init; }

    /// <summary>Тип вопроса.</summary>
    public required QuestionType Type { get; init; }
    /// <summary>Варианты ответов (для SingleChoice и MultipleChoice).</summary>
    public List<string> Options { get; init; } = [];

    /// <summary>Правильные ответы.</summary>
    public List<string> CorrectAnswers { get; init; } = [];
}