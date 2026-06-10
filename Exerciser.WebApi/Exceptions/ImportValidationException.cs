using System;

namespace Exerciser.WebApi.Exceptions;

/// <summary>Исключение для ошибок валидации данных при импорте.</summary>
public class ImportValidationException : Exception
{
    /// <summary>Инициализирует новый экземпляр ImportValidationException.</summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public ImportValidationException(string message) : base(message) { }

    /// <summary>Инициализирует новый экземпляр ImportValidationException.</summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Исходное исключение.</param>
    public ImportValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}