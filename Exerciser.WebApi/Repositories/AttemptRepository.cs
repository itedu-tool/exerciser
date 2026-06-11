using System;
using System.Threading.Tasks;

using Exerciser.WebApi.Models;

using MongoDB.Driver;

namespace Exerciser.WebApi.Repositories;

public interface IAttemptRepository
{
    Task CreateAsync(Attempt attempt);
    Task<Attempt?> GetByIdAsync(Guid id);
    Task UpdateAsync(Attempt attempt);
    Task<Attempt?> GetLatestUnfinishedAsync(Guid sessionId, Guid examId);
}

public class AttemptRepository : IAttemptRepository
{
    private readonly IMongoCollection<Attempt> _attempts;

    public AttemptRepository(IMongoDatabase database)
    {
        _attempts = database.GetCollection<Attempt>("Attempts");
    }

    public async Task CreateAsync(Attempt attempt)
    {
        await _attempts.InsertOneAsync(attempt);
    }

    public async Task<Attempt?> GetByIdAsync(Guid id)
    {
        return await _attempts.Find(a => a.Id == id).FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(Attempt attempt)
    {
        var filter = Builders<Attempt>.Filter.Eq(a => a.Id, attempt.Id);
        await _attempts.ReplaceOneAsync(filter, attempt);
    }

    public async Task<Attempt?> GetLatestUnfinishedAsync(Guid sessionId, Guid examId)
    {
        return await _attempts.Find(a => a.SessionId == sessionId && a.Exam.Id == examId && a.FinishedAt == null)
            .SortByDescending(a => a.StartedAt)
            .FirstOrDefaultAsync();
    }
}