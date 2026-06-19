using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Exerciser.WebApi.Models;
using Exerciser.WebApi.Repositories;
using Exerciser.WebApi.Tests.Fixtures;

using MongoDB.Driver;

using Xunit;

namespace Exerciser.WebApi.Tests.Integration.Repositories;

public class ExamRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly ExamRepository _repository;

    public ExamRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _repository = new ExamRepository(_fixture.Database, "Exams");
        _fixture.ClearCollectionsAsync().Wait(); // очистка перед каждым тестом (можно улучшить)
    }

    [Fact]
    public async Task CreateAsync_Should_Insert_Exam()
    {
        Exam exam = new() { Title = "Test", Description = "Desc" };
        await _repository.CreateAsync(exam);

        Exam? found = await _repository.GetByIdAsync(exam.Id);
        Assert.NotNull(found);
        Assert.Equal(exam.Title, found.Title);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Exams()
    {
        Exam exam1 = new() { Title = "Exam1" };
        Exam exam2 = new() { Title = "Exam2" };
        await _repository.CreateAsync(exam1);
        await _repository.CreateAsync(exam2);

        List<Exam> all = await _repository.GetAllAsync();
        Assert.Equal(2, all.Count);
        Assert.Contains(all, e => e.Title == "Exam1");
        Assert.Contains(all, e => e.Title == "Exam2");
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Exam()
    {
        Exam exam = new() { Title = "Original" };
        await _repository.CreateAsync(exam);

        exam.Title = "Updated";
        await _repository.UpdateAsync(exam);

        Exam? found = await _repository.GetByIdAsync(exam.Id);
        Assert.Equal("Updated", found.Title);
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Exam()
    {
        Exam exam = new() { Title = "ToDelete" };
        await _repository.CreateAsync(exam);

        bool deleted = await _repository.DeleteAsync(exam.Id);
        Assert.True(deleted);

        Exam? found = await _repository.GetByIdAsync(exam.Id);
        Assert.Null(found);
    }
}