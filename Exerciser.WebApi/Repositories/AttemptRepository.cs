using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;

using Exerciser.WebApi.Models;
using Exerciser.WebApi.Exceptions;

namespace Exerciser.WebApi.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с попытками прохождения экзаменов.
/// </summary>
public interface IAttemptRepository
{
    /// <summary>
    /// Создать новую попытку.
    /// </summary>
    /// <param name="attempt">Объект попытки.</param>
    Task CreateAsync(Attempt attempt);

    /// <summary>
    /// Получить попытку по идентификатору.
    /// </summary>
    /// <param name="id">GUID попытки.</param>
    Task<Attempt?> GetByIdAsync(Guid id);

    /// <summary>
    /// Обновить существующую попытку.
    /// </summary>
    /// <param name="attempt">Объект попытки с обновлёнными данными.</param>
    Task UpdateAsync(Attempt attempt);

    /// <summary>
    /// Получить последнюю незавершённую попытку для указанных сессии и экзамена.
    /// </summary>
    /// <param name="sessionId">GUID сессии.</param>
    /// <param name="examId">GUID экзамена.</param>
    Task<Attempt?> GetLatestUnfinishedAsync(Guid sessionId, Guid examId);

    /// <summary>
    /// Получить последние завершённые попытки по каждому студенту и экзамену.
    /// </summary>
    Task<IEnumerable<Attempt>> GetLastFinishedAttemptsByStudentAndExamAsync();
}

/// <summary>
/// Реализация репозитория для работы с попытками в MongoDB.
/// </summary>
public class AttemptRepository : RepositoryBase<Attempt>, IAttemptRepository
{
    /// <summary>
    /// Инициализирует новый экземпляр репозитория попыток.
    /// </summary>
    /// <param name="database">База данных MongoDB.</param>
    public AttemptRepository(IMongoDatabase database)
        : base(database, "Attempts")
    {
    }

    /// <inheritdoc />
    protected override Guid GetId(Attempt entity)
    {
        return entity.Id;
    }

    /// <inheritdoc />
    public async Task<Attempt?> GetLatestUnfinishedAsync(Guid sessionId, Guid examId)
    {
        try
        {
            FilterDefinition<Attempt>? filter = Builders<Attempt>.Filter.And(
                Builders<Attempt>.Filter.Eq(a => a.SessionId, sessionId),
                Builders<Attempt>.Filter.Eq(a => a.Exam.Id, examId),
                Builders<Attempt>.Filter.Eq(a => a.FinishedAt, null)
            );
            return await _collection
                .Find(filter)
                .SortByDescending(a => a.StartedAt)
                .FirstOrDefaultAsync();
        }
        catch (MongoException ex)
        {
            throw new ExamDatabaseException("Ошибка при получении последней незавершённой попытки", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Attempt>> GetLastFinishedAttemptsByStudentAndExamAsync()
    {
        try
        {
            BsonDocument[] pipeline = new BsonDocument[]
            {
                new("$match", new BsonDocument("FinishedAt", new BsonDocument("$ne", BsonNull.Value))),
                new("$sort", new BsonDocument("FinishedAt", -1)),
                new("$group",
                    new BsonDocument
                    {
                        {
                            "_id",
                            new BsonDocument
                            {
                                { "studentName", "$Student.FullName" },
                                { "groupName", "$Student.GroupName" },
                                { "examId", "$Exam.Id" },
                                { "examTitle", "$Exam.Title" }
                            }
                        },
                        { "doc", new BsonDocument("$first", "$$ROOT") }
                    }),
                new("$replaceRoot", new BsonDocument("newRoot", "$doc"))
            };

            return await _collection.Aggregate<Attempt>(pipeline).ToListAsync();
        }
        catch (MongoException ex)
        {
            throw new ExamDatabaseException("Ошибка при получении аналитики по последним попыткам", ex);
        }
    }
}