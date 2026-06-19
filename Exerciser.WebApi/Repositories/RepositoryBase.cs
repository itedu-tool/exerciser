using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MongoDB.Driver;

using Exerciser.WebApi.Exceptions;

namespace Exerciser.WebApi.Repositories;

/// <summary>
/// Базовый репозиторий для работы с MongoDB.
/// </summary>
/// <typeparam name="T">Тип сущности.</typeparam>
public abstract class RepositoryBase<T> where T : class
{
    protected readonly IMongoCollection<T> _collection;

    protected RepositoryBase(IMongoDatabase database, string collectionName)
    {
        _collection = database.GetCollection<T>(collectionName);
    }

    /// <summary>
    /// Получить все сущности.
    /// </summary>
    public virtual async Task<List<T>> GetAllAsync()
    {
        try
        {
            return await _collection.Find(_ => true).ToListAsync();
        }
        catch (MongoException ex)
        {
            throw new ExamDatabaseException("Ошибка при получении всех записей", ex);
        }
    }

    /// <summary>
    /// Получить сущность по идентификатору.
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        try
        {
            FilterDefinition<T>? filter = Builders<T>.Filter.Eq("_id", id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (MongoException ex)
        {
            throw new ExamDatabaseException($"Ошибка при получении записи с ID {id}", ex);
        }
    }

    /// <summary>
    /// Создать новую сущность.
    /// </summary>
    public virtual async Task CreateAsync(T entity)
    {
        try
        {
            await _collection.InsertOneAsync(entity);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ExamDatabaseException("Запись с таким идентификатором уже существует", ex);
        }
        catch (MongoException ex)
        {
            throw new ExamDatabaseException("Ошибка при создании записи", ex);
        }
    }

    /// <summary>
    /// Обновить сущность.
    /// </summary>
    public virtual async Task UpdateAsync(T entity)
    {
        try
        {
            FilterDefinition<T>? filter = Builders<T>.Filter.Eq("_id", GetId(entity));
            await _collection.ReplaceOneAsync(filter, entity);
        }
        catch (MongoException ex)
        {
            throw new ExamDatabaseException("Ошибка при обновлении записи", ex);
        }
    }

    /// <summary>
    /// Удалить сущность по идентификатору.
    /// </summary>
    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            FilterDefinition<T>? filter = Builders<T>.Filter.Eq("_id", id);
            DeleteResult? result = await _collection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
        catch (MongoException ex)
        {
            throw new ExamDatabaseException($"Ошибка при удалении записи с ID {id}", ex);
        }
    }

    /// <summary>
    /// Получить значение идентификатора из сущности.
    /// </summary>
    protected abstract Guid GetId(T entity);
}