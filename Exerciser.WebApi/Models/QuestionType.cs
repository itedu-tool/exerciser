using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Exerciser.WebApi.Models;

/// <summary>Тип вопроса экзамена.</summary>
public enum QuestionType
{
    /// <summary>Выбор одного варианта.</summary>
    SingleChoice,

    /// <summary>Выбор нескольких вариантов.</summary>
    MultipleChoice,

    /// <summary>Ввод текста.</summary>
    TextInput
}
