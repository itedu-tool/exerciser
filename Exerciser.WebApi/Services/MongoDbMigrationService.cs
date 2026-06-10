using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MongoDB.Driver;

using Exerciser.WebApi.Models;

using Microsoft.Extensions.Logging;

namespace Exerciser.WebApi.Services;

/// <summary>Сервис для инициализации базы данных MongoDB (миграции и индексы).</summary>
public interface IMongoDbMigrationService
{
    /// <summary>Инициализировать базу данных (создать индексы, коллекции).</summary>
    Task InitializeAsync();
}

/// <summary>Реализация сервиса миграций MongoDB.</summary>
public class MongoDbMigrationService : IMongoDbMigrationService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbMigrationService> _logger;
    private readonly string _examsCollectionName;

    public MongoDbMigrationService(
        IMongoDatabase database,
        ILogger<MongoDbMigrationService> logger,
        string examsCollectionName = "Exams")
    {
        _database = database;
        _logger = logger;
        _examsCollectionName = examsCollectionName;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Инициализация базы данных MongoDB...");

            // Создать коллекцию Exams, если её нет
            IAsyncCursor<string>? collections = await _database.ListCollectionNamesAsync();
            List<string>? collectionNames = await collections.ToListAsync();

            if (!collectionNames.Contains(_examsCollectionName))
            {
                await _database.CreateCollectionAsync(_examsCollectionName);
                _logger.LogInformation("✓ Коллекция '{CollectionName}' создана", _examsCollectionName);
            }

            IMongoCollection<Exam>? examsCollection = _database.GetCollection<Exam>(_examsCollectionName);

            // Создание индексов для оптимизации запросов
            await CreateExamIndexesAsync(examsCollection);

            _logger.LogInformation("✓ База данных MongoDB успешно инициализирована");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Ошибка при инициализации MongoDB");
            throw;
        }
    }

    /// <summary>Создать индексы для коллекции Exams.</summary>
    private async Task CreateExamIndexesAsync(IMongoCollection<Exam> collection)
    {
        try
        {
            // Индекс на Title для быстрого поиска экзаменов по названию
            CreateIndexModel<Exam> titleIndexModel = new(
                Builders<Exam>.IndexKeys.Text(e => e.Title),
                new CreateIndexOptions { Name = "idx_title_text" });

            // Индекс на CreatedAt для сортировки по дате создания
            CreateIndexModel<Exam> createdAtIndexModel = new(
                Builders<Exam>.IndexKeys.Descending(e => e.CreatedAt),
                new CreateIndexOptions { Name = "idx_created_at_desc" });

            // TTL индекс: автоматически удалять старые экзамены через 1 год (опционально, закомментировано)
            // var ttlIndexModel = new CreateIndexModel<Exam>(
            //     Builders<Exam>.IndexKeys.Ascending(e => e.CreatedAt),
            //     new CreateIndexOptions
            //     {
            //         Name = "idx_created_at_ttl",
            //         ExpireAfter = TimeSpan.FromDays(365)
            //     });

            await collection.Indexes.CreateManyAsync([titleIndexModel, createdAtIndexModel]);

            _logger.LogInformation("✓ Индексы для коллекции '{CollectionName}' созданы", _examsCollectionName);
        }
        catch (MongoCommandException ex) when (ex.Code == 85)
        {
            // Индекс с таким именем уже существует - это нормально
            _logger.LogInformation("Индексы уже существуют для коллекции '{CollectionName}'", _examsCollectionName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Ошибка при создании индексов MongoDB");
            throw;
        }
    }
}