using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Exerciser.WebApi.Models;

namespace Exerciser.WebApi.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с сессиями студентов.
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// Создать новую сессию.
    /// </summary>
    /// <param name="session">Объект сессии.</param>
    Task CreateAsync(Session session);

    /// <summary>
    /// Получить сессию по идентификатору.
    /// </summary>
    /// <param name="id">GUID сессии.</param>
    Task<Session?> GetByIdAsync(Guid id);
}

/// <summary>
/// Реализация репозитория для работы с сессиями в MongoDB.
/// </summary>
public class SessionRepository : RepositoryBase<Session>, ISessionRepository
{
    /// <summary>
    /// Инициализирует новый экземпляр репозитория сессий.
    /// </summary>
    /// <param name="database">База данных MongoDB.</param>
    public SessionRepository(IMongoDatabase database)
        : base(database, "Sessions")
    {
    }

    /// <inheritdoc />
    protected override Guid GetId(Session entity) => entity.Id;
}