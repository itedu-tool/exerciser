using System;
using System.Collections.Generic;

using MongoDB.Bson.Serialization.Attributes;

namespace Exerciser.WebApi.Models;

/// <summary>Экзамен/тест, сохранённый в MongoDB.</summary>
public record Exam
{
    /// <summary>Уникальный идентификатор (UUID v7) - MongoDB _id field.</summary>
    [BsonId]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Название экзамена.</summary>
    public required string Title { get; set; }

    /// <summary>Описание экзамена (необязательно).</summary>
    public string? Description { get; set; }

    /// <summary>Список вопросов экзамена.</summary>
    public List<Question> Questions { get; set; } = [];

    /// <summary>Дата и время создания (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Количество вопросов типа SingleChoice для показа (0 = все).</summary>
    public int SingleChoiceToShow { get; set; }

    /// <summary>Количество вопросов типа MultipleChoice для показа (0 = все).</summary>
    public int MultipleChoiceToShow { get; set; }

    /// <summary>Количество вопросов типа TextInput для показа (0 = все).</summary>
    public int TextInputToShow { get; set; }
}