namespace Exerciser.WebApi.DTOs;

/// <summary>Запрос на добавление студента в группу.</summary>
public record AddStudentToGroupRequest
{
    /// <summary>Фамилия.</summary>
    public required string LastName { get; init; }

    /// <summary>Имя.</summary>
    public required string FirstName { get; init; }

    /// <summary>Отчество (необязательно).</summary>
    public string? Patronymic { get; init; }
}