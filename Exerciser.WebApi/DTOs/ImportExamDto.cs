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

    /// <summary>Количество вопросов типа SingleChoice для показа (0 = все).</summary>
    public int SingleChoiceToShow { get; set; }

    /// <summary>Количество вопросов типа MultipleChoice для показа (0 = все).</summary>
    public int MultipleChoiceToShow { get; set; }

    /// <summary>Количество вопросов типа TextInput для показа (0 = все).</summary>
    public int TextInputToShow { get; set; }
}