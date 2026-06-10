namespace Exerciser.WebApi.DTOs;

/// <summary>DTO ответа при импорте экзамена.</summary>
public record ExamImportResponseDto
{
    /// <summary>ID созданного экзамена.</summary>
    public required string Id { get; set; }

    /// <summary>Название экзамена.</summary>
    public required string Title { get; set; }

    /// <summary>Количество вопросов.</summary>
    public int QuestionsCount { get; set; }
}