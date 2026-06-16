using System.Collections.Generic;

namespace Exerciser.WebApi.DTOs;

/// <summary>Запрос на импорт группы и студентов из JSON-файла.</summary>
public record ImportGroupRequest
{
    /// <summary>Название группы.</summary>
    public required string Name { get; init; }

    /// <summary>Список студентов (опционально).</summary>
    public List<ImportStudentDto> Students { get; init; } = [];
}

/// <summary>Студент при импорте.</summary>
public record ImportStudentDto
{
    /// <summary>Фамилия.</summary>
    public required string LastName { get; init; }

    /// <summary>Имя.</summary>
    public required string FirstName { get; init; }

    /// <summary>Отчество (необязательно).</summary>
    public string? Patronymic { get; init; }
}