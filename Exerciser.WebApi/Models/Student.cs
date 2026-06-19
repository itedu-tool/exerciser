using System;

using MongoDB.Bson.Serialization.Attributes;

namespace Exerciser.WebApi.Models;

/// <summary>Студент (вложенный документ в группе).</summary>
public record Student
{
    /// <summary>Уникальный идентификатор студента.</summary>
    [BsonId]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Фамилия.</summary>
    public required string LastName { get; set; }

    /// <summary>Имя.</summary>
    public required string FirstName { get; set; }

    /// <summary>Отчество (необязательно).</summary>
    public string? Patronymic { get; set; }

    /// <summary>Полное имя (вычисляемое поле).</summary>
    public string FullName => $"{LastName} {FirstName}{(Patronymic is not null ? " " + Patronymic : string.Empty)}";
}