using System;
using System.Threading.Tasks;

namespace Exerciser.WebApi.Services;

/// <summary>Интерфейс для кеширования данных приложения.</summary>
public interface ICacheService
{
    /// <summary>Получить значение из кеша.</summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>Сохранить значение в кеш.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>Удалить значение из кеша.</summary>
    Task RemoveAsync(string key);

    /// <summary>Удалить все значения по префиксу.</summary>
    Task RemoveByPrefixAsync(string prefix);
}