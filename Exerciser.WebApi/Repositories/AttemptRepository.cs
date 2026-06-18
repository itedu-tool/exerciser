using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Exerciser.WebApi.Models;

using MongoDB.Bson;
using MongoDB.Driver;

namespace Exerciser.WebApi.Repositories;

public interface IAttemptRepository
{
    Task CreateAsync(Attempt attempt);
    Task<Attempt?> GetByIdAsync(Guid id);
    Task UpdateAsync(Attempt attempt);
    Task<Attempt?> GetLatestUnfinishedAsync(Guid sessionId, Guid examId);
    Task<IEnumerable<Attempt>> GetLastFinishedAttemptsByStudentAndExamAsync();
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
    
    public async Task<IEnumerable<Attempt>> GetLastFinishedAttemptsByStudentAndExamAsync()
    {
        var pipeline = new[]
        {
            // Фильтр: только завершённые попытки
            new BsonDocument("$match", new BsonDocument("FinishedAt", new BsonDocument("$ne", BsonNull.Value))),
            // Сортировка по убыванию даты завершения
            new BsonDocument("$sort", new BsonDocument("FinishedAt", -1)),
            // Группировка по студенту + группе + экзамену
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "studentName", "$Student.FullName" },
                        { "groupName", "$Student.GroupName" },
                        { "examId", "$Exam.Id" },
                        { "examTitle", "$Exam.Title" }
                    }
                },
                { "doc", new BsonDocument("$first", "$$ROOT") }
            }),
            // Замена на документ
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$doc"))
        };

        return await _attempts.Aggregate<Attempt>(pipeline).ToListAsync();
    }
}