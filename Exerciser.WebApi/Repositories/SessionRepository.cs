using System;
using System.Threading.Tasks;

using Exerciser.WebApi.Models;

using MongoDB.Driver;

namespace Exerciser.WebApi.Repositories;

public interface ISessionRepository
{
    Task CreateAsync(Session session);
    Task<Session?> GetByIdAsync(Guid id);
}

public class SessionRepository : ISessionRepository
{
    private readonly IMongoCollection<Session> _sessions;

    public SessionRepository(IMongoDatabase database)
    {
        _sessions = database.GetCollection<Session>("Sessions");
    }

    public async Task CreateAsync(Session session)
    {
        await _sessions.InsertOneAsync(session);
    }

    public async Task<Session?> GetByIdAsync(Guid id)
    {
        return await _sessions.Find(s => s.Id == id).FirstOrDefaultAsync();
    }
}