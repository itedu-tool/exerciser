using System;
using System.Collections.Generic;

using Exerciser.WebApi.Models;

namespace Exerciser.WebApi.DTOs;

/// <summary>Ответ при создании новой попытки.</summary>
public record StartAttemptResponse
{
    /// <summary>Идентификатор попытки.</summary>
    public Guid AttemptId { get; init; }

    /// <summary>Снимок экзамена (вопросы, ответы).</summary>
    public required ExamSnapshotDto Exam { get; init; }
}

/// <summary>Снимок экзамена для передачи клиенту.</summary>
public record ExamSnapshotDto
{
    /// <summary>Идентификатор экзамена.</summary>
    public Guid Id { get; init; }

    /// <summary>Название экзамена.</summary>
    public required string Title { get; init; }

    /// <summary>Описание экзамена.</summary>
    public string? Description { get; init; }

    /// <summary>Список вопросов.</summary>
    public List<QuestionSnapshotDto> Questions { get; init; } = [];
}

/// <summary>Снимок вопроса для передачи клиенту (без правильных ответов).</summary>
public record QuestionSnapshotDto
{
    /// <summary>Идентификатор вопроса.</summary>
    public Guid Id { get; init; }

    /// <summary>Текст вопроса.</summary>
    public required string Text { get; init; }

    /// <summary>Тип вопроса.</summary>
    public required QuestionType Type { get; init; }

    /// <summary>Варианты ответов.</summary>
    public List<string> Options { get; init; } = [];
}