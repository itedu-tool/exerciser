using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MongoDB.Driver;

using Exerciser.WebApi.Models;

namespace Exerciser.WebApi.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с экзаменами в MongoDB.
/// </summary>
public interface IExamRepository
{
    /// <summary>Сохранить новый экзамен в базе данных.</summary>
    /// <param name="exam">Объект экзамена.</param>
    Task CreateAsync(Exam exam);

    /// <summary>Получить все экзамены из коллекции.</summary>
    /// <returns>Список всех экзаменов. Если экзамены отсутствуют, возвращается пустой список.</returns>
    Task<List<Exam>> GetAllAsync();

    /// <summary>Получить экзамен по его уникальному идентификатору.</summary>
    /// <param name="id">GUID экзамена.</param>
    /// <returns>Объект экзамена или <c>null</c>, если экзамен с указанным ID не найден.</returns>
    Task<Exam?> GetByIdAsync(Guid id);

    /// <summary>Обновить существующий экзамен в базе данных.</summary>
    /// <param name="exam">Объект экзамена с обновлёнными данными. Экзамен с таким же идентификатором должен существовать.</param>
    Task UpdateAsync(Exam exam);

    /// <summary>Удалить экзамен из базы данных по его идентификатору.</summary>
    /// <param name="id">GUID экзамена.</param>
    /// <returns><c>true</c>, если экзамен был удалён; <c>false</c>, если экзамен с таким ID не найден.</returns>
    Task<bool> DeleteAsync(Guid id);
}

/// <summary>
/// Реализация репозитория экзаменов на основе MongoDB.
/// </summary>
public class ExamRepository : IExamRepository
{
    private readonly IMongoCollection<Exam> _exams;

    /// <summary>
    /// Инициализирует новый экземпляр репозитория экзаменов.
    /// </summary>
    /// <param name="database">База данных MongoDB, из которой будет получена коллекция.</param>
    /// <param name="collectionName">Имя коллекции, содержащей экзамены.</param>
    public ExamRepository(IMongoDatabase database, string collectionName)
    {
        _exams = database.GetCollection<Exam>(collectionName);
    }

    /// <inheritdoc />
    /// <exception cref="MongoWriteException">Выбрасывается при ошибке записи в MongoDB (например, дублирование ключа, нарушение схемы).</exception>
    /// <exception cref="MongoConnectionException">Выбрасывается при проблемах с подключением к серверу MongoDB.</exception>
    public async Task CreateAsync(Exam exam)
    {
        await _exams.InsertOneAsync(exam);
    }

    /// <inheritdoc />
    /// <exception cref="MongoConnectionException">Выбрасывается при проблемах с подключением к серверу MongoDB.</exception>
    public async Task<List<Exam>> GetAllAsync()
    {
        return await _exams.Find(_ => true).ToListAsync();
    }

    /// <inheritdoc />
    /// <exception cref="MongoConnectionException">Выбрасывается при проблемах с подключением к серверу MongoDB.</exception>
    public async Task<Exam?> GetByIdAsync(Guid id)
    {
        FilterDefinition<Exam>? filter = Builders<Exam>.Filter.Eq(e => e.Id, id);
        return await _exams.Find(filter).FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    /// <exception cref="MongoConnectionException">Выбрасывается при проблемах с подключением к серверу MongoDB.</exception>
    public async Task UpdateAsync(Exam exam)
    {
        var filter = Builders<Exam>.Filter.Eq(e => e.Id, exam.Id);
        await _exams.ReplaceOneAsync(filter, exam);
    }

    /// <inheritdoc />
    /// <exception cref="MongoConnectionException">Выбрасывается при проблемах с подключением к серверу MongoDB.</exception>
    public async Task<bool> DeleteAsync(Guid id)
    {
        FilterDefinition<Exam>? filter = Builders<Exam>.Filter.Eq(e => e.Id, id);
        DeleteResult? result = await _exams.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }
}