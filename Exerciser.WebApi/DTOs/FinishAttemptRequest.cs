using System;
using System.Collections.Generic;

namespace Exerciser.WebApi.DTOs;

/// <summary>Запрос на завершение попытки (подсчёт баллов — на сервере).</summary>
public record FinishAttemptRequest
{
    /// <summary>Время завершения (UTC).</summary>
    public DateTime FinishedAt { get; init; }

    /// <summary>Список ответов студента.</summary>
    public List<AnswerSubmissionDto> Answers { get; init; } = [];
}

/// <summary>Ответ студента на один вопрос (без баллов — сервер подсчитает).</summary>
public record AnswerSubmissionDto
{
    /// <summary>Идентификатор вопроса.</summary>
    public Guid QuestionId { get; init; }

    /// <summary>Значение ответа (строка, массив строк, null).</summary>
    public object? Answer { get; init; }
}