using System;

namespace Exerciser.WebApi.DTOs;

/// <summary>Запрос на начало новой попытки прохождения экзамена.</summary>
public record StartAttemptRequest
{
    /// <summary>Идентификатор экзамена.</summary>
    public required Guid ExamId { get; init; }
}