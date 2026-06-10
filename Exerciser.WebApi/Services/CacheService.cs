using System;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

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

/// <summary>Реализация кеш-сервиса на основе IDistributedCache.</summary>
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private const string CacheKeyPrefix = "exerciser:";

    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            string cacheKey = FormatKey(key);
            string? cachedValue = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache MISS for key: {Key}", key);
                return default;
            }

            T? result = JsonSerializer.Deserialize<T>(cachedValue);
            _logger.LogDebug("Cache HIT for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving cache for key: {Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            if (value == null)
            {
                await RemoveAsync(key);
                return;
            }

            string cacheKey = FormatKey(key);
            string serializedValue = JsonSerializer.Serialize(value);

            DistributedCacheEntryOptions cacheOptions = new();
            if (expiration.HasValue)
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = expiration;
            }
            else
            {
                // Default expiration: 1 hour
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            }

            await _cache.SetStringAsync(cacheKey, serializedValue, cacheOptions);
            _logger.LogDebug("Cache SET for key: {Key}, expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache for key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key)
    {
        try
        {
            string cacheKey = FormatKey(key);
            await _cache.RemoveAsync(cacheKey);
            _logger.LogDebug("Cache REMOVED for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cache for key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveByPrefixAsync(string prefix)
    {
        // TODO: Для MemoryCache это невозможно реализовать напрямую. Для Redis можно использовать KEYS команду. Пока просто логируем
        _logger.LogWarning("RemoveByPrefixAsync не полностью реализовано для текущего кеша");
        await Task.CompletedTask;
    }

    /// <summary>Форматировать ключ кеша с префиксом.</summary>
    private string FormatKey(string key)
    {
        return $"{CacheKeyPrefix}{key}";
    }
}