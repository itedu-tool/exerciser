using System;

namespace Exerciser.WebApi.Models;

/// <summary>Сохранённый ответ студента в рамках попытки.</summary>
public record StoredAnswer
{
    /// <summary>Идентификатор вопроса.</summary>
    public Guid QuestionId { get; set; } = Guid.CreateVersion7();

    /// <summary>Значение ответа (строка, массив строк или null).</summary>
    public object? AnswerValue { get; set; }

    /// <summary>Количество баллов, полученных за этот ответ.</summary>
    public int Score { get; set; }

    /// <summary>Время сохранения ответа (UTC).</summary>
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}