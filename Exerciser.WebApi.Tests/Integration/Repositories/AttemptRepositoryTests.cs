using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Exerciser.WebApi.Models;
using Exerciser.WebApi.Repositories;
using Exerciser.WebApi.Tests.Fixtures;

using Xunit;

namespace Exerciser.WebApi.Tests.Integration.Repositories;

public class AttemptRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly AttemptRepository _repository;

    public AttemptRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repository = new AttemptRepository(_fixture.Database);
        _fixture.ClearCollectionsAsync().Wait();
    }

    [Fact]
    public async Task GetLastFinishedAttemptsByStudentAndExamAsync_Should_Return_Last_Attempt_Per_Student_And_Exam()
    {
        // Arrange
        StudentSnapshot student = new() { FullName = "Ivanov Ivan", GroupName = "Group1" };
        Guid examId = Guid.NewGuid();
        ExamSnapshot exam = new()
        {
            Id = examId, Title = "Exam1", Questions = new System.Collections.Generic.List<QuestionSnapshot>()
        };

        Attempt attempt1 = new()
        {
            SessionId = Guid.NewGuid(),
            Student = student,
            Exam = exam,
            StartedAt = DateTime.UtcNow.AddHours(-2),
            FinishedAt = DateTime.UtcNow.AddHours(-2),
            TotalScore = 10
        };
        Attempt attempt2 = new()
        {
            SessionId = Guid.NewGuid(),
            Student = student,
            Exam = exam,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            FinishedAt = DateTime.UtcNow.AddHours(-1),
            TotalScore = 20
        };

        await _repository.CreateAsync(attempt1);
        await _repository.CreateAsync(attempt2);

        // Act
        IEnumerable<Attempt> results = await _repository.GetLastFinishedAttemptsByStudentAndExamAsync();
        List<Attempt> list = results.ToList();

        // Assert
        Assert.Single(list);
        Attempt last = list.First();
        Assert.Equal(attempt2.Id, last.Id);
        Assert.Equal(20, last.TotalScore);
    }

    [Fact]
    public async Task GetLatestUnfinishedAsync_Should_Return_Only_Unfinished_Attempt()
    {
        Guid sessionId = Guid.NewGuid();
        Guid examId = Guid.NewGuid();
        StudentSnapshot student = new() { FullName = "Petrov Petr", GroupName = "Group2" };
        ExamSnapshot exam = new()
        {
            Id = examId, Title = "Exam2", Questions = new System.Collections.Generic.List<QuestionSnapshot>()
        };

        Attempt unfinished = new()
        {
            SessionId = sessionId,
            Student = student,
            Exam = exam,
            StartedAt = DateTime.UtcNow,
            FinishedAt = null
        };
        Attempt finished = new()
        {
            SessionId = sessionId,
            Student = student,
            Exam = exam,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            FinishedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        await _repository.CreateAsync(unfinished);
        await _repository.CreateAsync(finished);

        Attempt? result = await _repository.GetLatestUnfinishedAsync(sessionId, examId);
        Assert.NotNull(result);
        Assert.Null(result.FinishedAt);
        Assert.Equal(unfinished.Id, result.Id);
    }
}