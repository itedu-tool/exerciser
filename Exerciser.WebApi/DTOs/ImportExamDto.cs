using System.Collections.Generic;

namespace Exerciser.WebApi.DTOs;

/// <summary>DTO для импорта экзамена из JSON файла.</summary>
public record ImportExamDto
{
    /// <summary>Название экзамена.</summary>
    public required string Title { get; set; }

    /// <summary>Описание экзамена (необязательно).</summary>
    public string? Description { get; set; }

    /// <summary>Список вопросов.</summary>
    public List<ImportQuestionDto> Questions { get; set; } = [];
}