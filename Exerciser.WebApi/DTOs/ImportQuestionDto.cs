using System.Collections.Generic;

namespace Exerciser.WebApi.DTOs;

/// <summary>DTO вопроса при импорте экзамена.</summary>
public record ImportQuestionDto
{
    /// <summary>Текст вопроса.</summary>
    public required string Text { get; set; }

    /// <summary>Тип вопроса (SingleChoice, MultipleChoice, TextInput).</summary>
    public required string Type { get; set; }

    /// <summary>Варианты ответов (обязателен для Single/MultipleChoice).</summary>
    public List<string>? Options { get; set; }

    /// <summary>Список правильных ответов.</summary>
    public List<string> CorrectAnswers { get; set; } = [];
}