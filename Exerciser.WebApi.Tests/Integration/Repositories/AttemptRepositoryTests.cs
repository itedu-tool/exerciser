using System;
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
        var student = new StudentSnapshot { FullName = "Ivanov Ivan", GroupName = "Group1" };
        var examId = Guid.NewGuid();
        var exam = new ExamSnapshot { Id = examId, Title = "Exam1", Questions = new System.Collections.Generic.List<QuestionSnapshot>() };

        var attempt1 = new Attempt
        {
            SessionId = Guid.NewGuid(),
            Student = student,
            Exam = exam,
            StartedAt = DateTime.UtcNow.AddHours(-2),
            FinishedAt = DateTime.UtcNow.AddHours(-2),
            TotalScore = 10
        };
        var attempt2 = new Attempt
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
        var results = await _repository.GetLastFinishedAttemptsByStudentAndExamAsync();
        var list = results.ToList();

        // Assert
        Assert.Single(list);
        var last = list.First();
        Assert.Equal(attempt2.Id, last.Id);
        Assert.Equal(20, last.TotalScore);
    }

    [Fact]
    public async Task GetLatestUnfinishedAsync_Should_Return_Only_Unfinished_Attempt()
    {
        var sessionId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var student = new StudentSnapshot { FullName = "Petrov Petr", GroupName = "Group2" };
        var exam = new ExamSnapshot { Id = examId, Title = "Exam2", Questions = new System.Collections.Generic.List<QuestionSnapshot>() };

        var unfinished = new Attempt
        {
            SessionId = sessionId,
            Student = student,
            Exam = exam,
            StartedAt = DateTime.UtcNow,
            FinishedAt = null
        };
        var finished = new Attempt
        {
            SessionId = sessionId,
            Student = student,
            Exam = exam,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            FinishedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        await _repository.CreateAsync(unfinished);
        await _repository.CreateAsync(finished);

        var result = await _repository.GetLatestUnfinishedAsync(sessionId, examId);
        Assert.NotNull(result);
        Assert.Null(result.FinishedAt);
        Assert.Equal(unfinished.Id, result.Id);
    }
}