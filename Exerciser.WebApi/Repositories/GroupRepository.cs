using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Exerciser.WebApi.Models;

namespace Exerciser.WebApi.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с группами студентов.
/// </summary>
public interface IGroupRepository
{
    /// <summary>
    /// Получить все группы.
    /// </summary>
    Task<List<Group>> GetAllAsync();

    /// <summary>
    /// Получить группу по идентификатору.
    /// </summary>
    /// <param name="id">GUID группы.</param>
    Task<Group?> GetByIdAsync(Guid id);

    /// <summary>
    /// Создать новую группу.
    /// </summary>
    /// <param name="group">Объект группы.</param>
    Task CreateAsync(Group group);

    /// <summary>
    /// Обновить существующую группу.
    /// </summary>
    /// <param name="group">Объект группы с обновлёнными данными.</param>
    Task UpdateAsync(Group group);

    /// <summary>
    /// Удалить группу по идентификатору.
    /// </summary>
    /// <param name="id">GUID группы.</param>
    Task<bool> DeleteAsync(Guid id);
}

/// <summary>
/// Реализация репозитория для работы с группами в MongoDB.
/// </summary>
public class GroupRepository : RepositoryBase<Group>, IGroupRepository
{
    /// <summary>
    /// Инициализирует новый экземпляр репозитория групп.
    /// </summary>
    /// <param name="database">База данных MongoDB.</param>
    public GroupRepository(IMongoDatabase database)
        : base(database, "Groups")
    {
    }

    /// <inheritdoc />
    protected override Guid GetId(Group entity) => entity.Id;
}