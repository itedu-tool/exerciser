using System;
using System.Collections.Generic;

using Exerciser.WebApi.Models;

namespace Exerciser.WebApi.DTOs;

/// <summary>Результат завершённой попытки для отображения студенту.</summary>
public record AttemptResultDto
{
    /// <summary>Идентификатор попытки.</summary>
    public Guid AttemptId { get; init; }

    /// <summary>Название экзамена.</summary>
    public required string ExamTitle { get; init; }

    /// <summary>Полное имя студента.</summary>
    public required string StudentFullName { get; init; }

    /// <summary>Название группы.</summary>
    public required string GroupName { get; init; }

    /// <summary>Время начала попытки (UTC).</summary>
    public DateTime StartedAt { get; init; }

    /// <summary>Время окончания попытки (UTC).</summary>
    public DateTime FinishedAt { get; init; }

    /// <summary>Итоговый балл.</summary>
    public int TotalScore { get; init; }

    /// <summary>Максимально возможный балл.</summary>
    public int MaxPossibleScore { get; init; }

    /// <summary>Детали по каждому вопросу.</summary>
    public List<QuestionResultDto> Questions { get; init; } = [];
}

/// <summary>Результат по одному вопросу.</summary>
public record QuestionResultDto
{
    /// <summary>Текст вопроса.</summary>
    public required string Text { get; init; }

    /// <summary>Тип вопроса.</summary>
    public required QuestionType Type { get; init; }

    /// <summary>Варианты ответов (если применимо).</summary>
    public List<string> Options { get; init; } = [];

    /// <summary>Правильные ответы.</summary>
    public List<string> CorrectAnswers { get; init; } = [];

    /// <summary>Ответ студента.</summary>
    public object? UserAnswer { get; init; }

    /// <summary>Полученные баллы.</summary>
    public int Score { get; init; }

    /// <summary>Максимальный балл за вопрос.</summary>
    public int MaxScore { get; init; }
}