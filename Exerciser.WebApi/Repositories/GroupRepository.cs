using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Exerciser.WebApi.Models;
using MongoDB.Driver;

namespace Exerciser.WebApi.Repositories;

/// <summary>Интерфейс репозитория для работы с группами и студентами.</summary>
public interface IGroupRepository
{
    /// <summary>Получить все группы.</summary>
    Task<List<Group>> GetAllAsync();

    /// <summary>Получить группу по идентификатору.</summary>
    /// <param name="id">GUID группы.</param>
    Task<Group?> GetByIdAsync(Guid id);

    /// <summary>Создать новую группу.</summary>
    Task CreateAsync(Group group);

    /// <summary>Обновить существующую группу (например, добавить студента).</summary>
    Task UpdateAsync(Group group);

    /// <summary>Удалить группу по идентификатору (опционально).</summary>
    Task<bool> DeleteAsync(Guid id);
}

/// <summary>Реализация репозитория групп на основе MongoDB.</summary>
public class GroupRepository : IGroupRepository
{
    private readonly IMongoCollection<Group> _groups;

    /// <summary>Инициализирует репозиторий с указанной коллекцией MongoDB.</summary>
    /// <param name="database">База данных MongoDB.</param>
    public GroupRepository(IMongoDatabase database)
    {
        _groups = database.GetCollection<Group>("Groups");
    }

    /// <inheritdoc />
    public async Task<List<Group>> GetAllAsync()
    {
        return await _groups.Find(_ => true).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Group?> GetByIdAsync(Guid id)
    {
        return await _groups.Find(g => g.Id == id).FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task CreateAsync(Group group)
    {
        await _groups.InsertOneAsync(group);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Group group)
    {
        var filter = Builders<Group>.Filter.Eq(g => g.Id, group.Id);
        await _groups.ReplaceOneAsync(filter, group);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id)
    {
        var filter = Builders<Group>.Filter.Eq(g => g.Id, id);
        var result = await _groups.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }
}