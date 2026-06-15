using System;
using System.Collections.Generic;

namespace Exerciser.WebApi.DTOs;

/// <summary>Запрос на завершение попытки.</summary>
public record FinishAttemptRequest
{
    /// <summary>Итоговый балл.</summary>
    public int TotalScore { get; init; }

    /// <summary>Время завершения (UTC).</summary>
    public DateTime FinishedAt { get; init; }

    /// <summary>Список ответов студента.</summary>
    public List<AnswerSubmissionDto> Answers { get; init; } = [];
}

/// <summary>Ответ студента на один вопрос.</summary>
public record AnswerSubmissionDto
{
    /// <summary>Идентификатор вопроса.</summary>
    public Guid QuestionId { get; init; }

    /// <summary>Значение ответа (строка, массив строк, null).</summary>
    public object? Answer { get; init; }

    /// <summary>Балл за этот ответ (вычислен на клиенте).</summary>
    public int Score { get; init; }
}