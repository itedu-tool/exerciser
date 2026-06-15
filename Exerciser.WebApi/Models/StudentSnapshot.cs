namespace Exerciser.WebApi.Models;

/// <summary>Снимок данных студента для сессии или попытки.</summary>
public record StudentSnapshot
{
    /// <summary>Полное имя студента.</summary>
    public required string FullName { get; set; }
    
    /// <summary>Название группы.</summary>
    public required string GroupName { get; set; }
}