using System.Collections.Generic;

namespace Exerciser.WebApi.DTOs;

/// <summary>Информация о группе со списком студентов.</summary>
public record GroupInfoDto
{
    /// <summary>Идентификатор группы (строка, так как это ObjectId).</summary>
    public required string Id { get; init; }

    /// <summary>Название группы.</summary>
    public required string Name { get; init; }

    /// <summary>Список студентов в группе.</summary>
    public List<StudentInfoDto> Students { get; init; } = [];
}

/// <summary>Информация о студенте.</summary>
public record StudentInfoDto
{
    /// <summary>Идентификатор студента (строка).</summary>
    public required string Id { get; init; }

    /// <summary>Полное имя студента.</summary>
    public required string FullName { get; init; }
}