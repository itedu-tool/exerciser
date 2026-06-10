using System;
using System.Collections.Generic;

using MongoDB.Bson.Serialization.Attributes;

namespace Exerciser.WebApi.Models;

/// <summary>Вопрос экзамена.</summary>
public record Question
{
    /// <summary>Уникальный идентификатор вопроса (UUID v7).</summary>
    [BsonId]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Текст вопроса.</summary>
    public required string Text { get; set; }

    /// <summary>Тип вопроса: SingleChoice, MultipleChoice, TextInput.</summary>
    public required string Type { get; set; }

    /// <summary>Варианты ответов (для SingleChoice и MultipleChoice).</summary>
    public List<string> Options { get; set; } = [];

    /// <summary>Правильные ответы (строки).</summary>
    public List<string> CorrectAnswers { get; set; } = [];
}