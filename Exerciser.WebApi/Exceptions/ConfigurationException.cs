using System;

namespace Exerciser.WebApi.Exceptions;

/// <summary>Исключение для ошибок конфигурации приложения.</summary>
public class ConfigurationException : Exception
{
    /// <summary>Инициализирует новый экземпляр ConfigurationException.</summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public ConfigurationException(string message) : base(message) { }

    /// <summary>Инициализирует новый экземпляр ConfigurationException.</summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Исходное исключение.</param>
    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}