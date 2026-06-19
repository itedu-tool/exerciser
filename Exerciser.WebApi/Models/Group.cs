using System;
using System.Collections.Generic;

using MongoDB.Bson.Serialization.Attributes;

namespace Exerciser.WebApi.Models;

/// <summary>Группа студентов.</summary>
public record Group
{
    /// <summary>Уникальный идентификатор группы.</summary>
    [BsonId]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Название группы.</summary>
    public required string Name { get; set; }

    /// <summary>Список студентов в группе (вложенные документы).</summary>
    public List<Student> Students { get; set; } = [];
}