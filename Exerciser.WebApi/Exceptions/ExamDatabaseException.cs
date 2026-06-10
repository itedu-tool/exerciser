using System;

namespace Exerciser.WebApi.Exceptions;

/// <summary>Исключение для ошибок при работе с MongoDB.</summary>
public class ExamDatabaseException : Exception
{
    /// <summary>Инициализирует новый экземпляр ExamDatabaseException.</summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public ExamDatabaseException(string message) : base(message) { }

    /// <summary>Инициализирует новый экземпляр ExamDatabaseException.</summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Исходное исключение.</param>
    public ExamDatabaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}