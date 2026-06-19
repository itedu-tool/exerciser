using System;
using System.Collections.Generic;

namespace Exerciser.WebApi.Models;

/// <summary>Снимок экзамена, используемый в попытке.</summary>
public record ExamSnapshot
{
    /// <summary>Идентификатор экзамена.</summary>
    public required Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Название экзамена.</summary>
    public required string Title { get; set; }

    /// <summary>Описание экзамена.</summary>
    public string? Description { get; set; }

    /// <summary>Список снимков вопросов.</summary>
    public required List<QuestionSnapshot> Questions { get; set; }
}

/// <summary>Снимок вопроса, используемый в попытке.</summary>
public record QuestionSnapshot
{
    /// <summary>Идентификатор вопроса.</summary>
    public required Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Текст вопроса.</summary>
    public required string Text { get; set; }

    /// <summary>Тип вопроса (SingleChoice, MultipleChoice, TextInput).</summary>
    public required string Type { get; set; }

    /// <summary>Варианты ответов (для Single/MultipleChoice).</summary>
    public List<string> Options { get; set; } = [];

    /// <summary>Правильные ответы.</summary>
    public List<string> CorrectAnswers { get; set; } = [];
}