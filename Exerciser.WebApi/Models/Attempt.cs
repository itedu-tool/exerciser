using System;
using System.Collections.Generic;

namespace Exerciser.WebApi.Models;

/// <summary>Попытка прохождения экзамена студентом.</summary>
public record Attempt
{
    /// <summary>Уникальный идентификатор попытки.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Идентификатор сессии, в рамках которой создана попытка.</summary>
    public required Guid SessionId { get; set; }

    /// <summary>Снимок данных студента на момент начала попытки.</summary>
    public required StudentSnapshot Student { get; set; }

    /// <summary>Снимок экзамена (вопросы, ответы) на момент начала попытки.</summary>
    public required ExamSnapshot Exam { get; set; }

    /// <summary>Время начала попытки (UTC).</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Время завершения попытки (UTC), null – попытка не завершена.</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Итоговая сумма баллов за все ответы.</summary>
    public int TotalScore { get; set; }

    /// <summary>Список сохранённых ответов студента.</summary>
    public List<StoredAnswer> Answers { get; set; } = [];
}