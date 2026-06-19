using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Exerciser.WebApi.Services;

/// <summary>Реализация кеш-сервиса на основе IDistributedCache.</summary>
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly IConnectionMultiplexer? _multiplexer;
    private const string CacheKeyPrefix = "exerciser:";

    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger,
        IConnectionMultiplexer? multiplexer = null)
    {
        _cache = cache;
        _logger = logger;
        _multiplexer = multiplexer;
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
        try
        {
            if (_multiplexer == null)
            {
                _logger.LogWarning("RemoveByPrefixAsync не поддерживается для MemoryCache (пропускаем)");
                return;
            }

            string pattern = $"{CacheKeyPrefix}{prefix}*";
            var endpoints = _multiplexer.GetEndPoints();
            if (endpoints.Length == 0)
            {
                _logger.LogWarning("Нет доступных Redis-эндпоинтов для удаления по префиксу");
                return;
            }

            var server = _multiplexer.GetServer(endpoints.First());
            var db = _multiplexer.GetDatabase();
            var keys = server.Keys(pattern: pattern);

            int deletedCount = 0;
            foreach (var key in keys)
            {
                await db.KeyDeleteAsync(key);
                deletedCount++;
            }

            if (deletedCount > 0)
                _logger.LogDebug("Удалено {Count} ключей по префиксу '{Prefix}'", deletedCount, prefix);
            else
                _logger.LogDebug("Ключи по префиксу '{Prefix}' не найдены", prefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка при удалении по префиксу '{Prefix}'", prefix);
        }
    }

    /// <summary>Форматировать ключ кеша с префиксом.</summary>
    private string FormatKey(string key)
    {
        return $"{CacheKeyPrefix}{key}";
    }
}