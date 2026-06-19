using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MongoDB.Driver;

using Exerciser.WebApi.Models;

namespace Exerciser.WebApi.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с экзаменами.
/// </summary>
public interface IExamRepository
{
    /// <summary>
    /// Создать новый экзамен.
    /// </summary>
    /// <param name="exam">Объект экзамена.</param>
    Task CreateAsync(Exam exam);

    /// <summary>
    /// Получить все экзамены.
    /// </summary>
    Task<List<Exam>> GetAllAsync();

    /// <summary>
    /// Получить экзамен по идентификатору.
    /// </summary>
    /// <param name="id">GUID экзамена.</param>
    Task<Exam?> GetByIdAsync(Guid id);

    /// <summary>
    /// Обновить существующий экзамен.
    /// </summary>
    /// <param name="exam">Объект экзамена с обновлёнными данными.</param>
    Task UpdateAsync(Exam exam);

    /// <summary>
    /// Удалить экзамен по идентификатору.
    /// </summary>
    /// <param name="id">GUID экзамена.</param>
    Task<bool> DeleteAsync(Guid id);
}

/// <summary>
/// Реализация репозитория для работы с экзаменами в MongoDB.
/// </summary>
public class ExamRepository : RepositoryBase<Exam>, IExamRepository
{
    /// <summary>
    /// Инициализирует новый экземпляр репозитория экзаменов.
    /// </summary>
    /// <param name="database">База данных MongoDB.</param>
    /// <param name="collectionName">Имя коллекции.</param>
    public ExamRepository(IMongoDatabase database, string collectionName)
        : base(database, collectionName)
    {
    }

    /// <inheritdoc />
    protected override Guid GetId(Exam entity)
    {
        return entity.Id;
    }
}