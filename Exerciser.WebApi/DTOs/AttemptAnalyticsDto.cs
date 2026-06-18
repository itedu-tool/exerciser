using System;

namespace Exerciser.WebApi.DTOs;

/// <summary>Данные для аналитики по одной попытке (последней завершённой).</summary>
public record AttemptAnalyticsDto
{
    /// <summary>Идентификатор попытки.</summary>
    public Guid AttemptId { get; init; }

    /// <summary>Полное имя студента.</summary>
    public required string StudentFullName { get; init; }

    /// <summary>Название группы.</summary>
    public required string GroupName { get; init; }

    /// <summary>Название экзамена.</summary>
    public required string ExamTitle { get; init; }

    /// <summary>Набранные баллы.</summary>
    public int TotalScore { get; init; }

    /// <summary>Максимально возможные баллы.</summary>
    public int MaxPossibleScore { get; init; }

    /// <summary>Процент правильных ответов (округлённый).</summary>
    public int Percent { get; init; }

    /// <summary>Дата и время завершения попытки.</summary>
    public DateTime FinishedAt { get; init; }

    /// <summary>Длительность попытки в минутах (округлённо).</summary>
    public int DurationMinutes { get; init; }
}