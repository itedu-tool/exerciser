using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Exerciser.WebApi.Controllers;
using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Models;
using Exerciser.WebApi.Repositories;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using Xunit;

namespace Exerciser.WebApi.Tests.Unit;

public class AttemptsControllerTests
{
    private readonly Mock<IAttemptRepository> _attemptRepositoryMock;
    private readonly Mock<IExamRepository> _examRepositoryMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly AttemptsController _controller;

    public AttemptsControllerTests()
    {
        _attemptRepositoryMock = new Mock<IAttemptRepository>();
        _examRepositoryMock = new Mock<IExamRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _controller = new AttemptsController(
            _attemptRepositoryMock.Object,
            _examRepositoryMock.Object,
            _sessionRepositoryMock.Object);
    }

    private static int GetTotalScore(object? response)
    {
        Assert.NotNull(response);
        PropertyInfo? prop = response.GetType().GetProperty("totalScore");
        Assert.NotNull(prop);
        return (int)prop.GetValue(response)!;
    }

    [Fact]
    public async Task Finish_SingleChoice_CorrectAnswer_Returns1Point()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();
        Guid questionId = Guid.NewGuid();

        Attempt attempt = new()
        {
            Id = attemptId,
            SessionId = sessionId,
            Student = new StudentSnapshot { FullName = "Test Student", GroupName = "Test Group" },
            Exam = new ExamSnapshot
            {
                Id = Guid.NewGuid(),
                Title = "Test Exam",
                Questions = new List<QuestionSnapshot>
                {
                    new()
                    {
                        Id = questionId,
                        Text = "Question 1",
                        Type = QuestionType.SingleChoice,
                        Options = new List<string> { "A", "B", "C" },
                        CorrectAnswers = new List<string> { "A" }
                    }
                }
            }
        };

        _attemptRepositoryMock.Setup(r => r.GetByIdAsync(attemptId)).ReturnsAsync(attempt);

        FinishAttemptRequest request = new()
        {
            FinishedAt = DateTime.UtcNow,
            Answers = new List<AnswerSubmissionDto>
            {
                new() { QuestionId = questionId, Answer = "A" }
            }
        };

        SetupSessionHeader(sessionId);

        // Act
        IActionResult result = await _controller.Finish(attemptId, request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        int totalScore = GetTotalScore(okResult.Value);
        Assert.Equal(1, totalScore);
    }

    [Fact]
    public async Task Finish_SingleChoice_WrongAnswer_Returns0Points()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();
        Guid questionId = Guid.NewGuid();

        Attempt attempt = new()
        {
            Id = attemptId,
            SessionId = sessionId,
            Student = new StudentSnapshot { FullName = "Test Student", GroupName = "Test Group" },
            Exam = new ExamSnapshot
            {
                Id = Guid.NewGuid(),
                Title = "Test Exam",
                Questions = new List<QuestionSnapshot>
                {
                    new()
                    {
                        Id = questionId,
                        Text = "Question 1",
                        Type = QuestionType.SingleChoice,
                        Options = new List<string> { "A", "B", "C" },
                        CorrectAnswers = new List<string> { "A" }
                    }
                }
            }
        };

        _attemptRepositoryMock.Setup(r => r.GetByIdAsync(attemptId)).ReturnsAsync(attempt);

        FinishAttemptRequest request = new()
        {
            FinishedAt = DateTime.UtcNow,
            Answers = new List<AnswerSubmissionDto>
            {
                new() { QuestionId = questionId, Answer = "B" }
            }
        };

        SetupSessionHeader(sessionId);

        // Act
        IActionResult result = await _controller.Finish(attemptId, request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        int totalScore = GetTotalScore(okResult.Value);
        Assert.Equal(0, totalScore);
    }

    [Fact]
    public async Task Finish_MultipleChoice_AllCorrect_ReturnsFullPoints()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();
        Guid questionId = Guid.NewGuid();

        Attempt attempt = new()
        {
            Id = attemptId,
            SessionId = sessionId,
            Student = new StudentSnapshot { FullName = "Test Student", GroupName = "Test Group" },
            Exam = new ExamSnapshot
            {
                Id = Guid.NewGuid(),
                Title = "Test Exam",
                Questions = new List<QuestionSnapshot>
                {
                    new()
                    {
                        Id = questionId,
                        Text = "Question 1",
                        Type = QuestionType.MultipleChoice,
                        Options = new List<string> { "A", "B", "C", "D" },
                        CorrectAnswers = new List<string> { "A", "B" }
                    }
                }
            }
        };

        _attemptRepositoryMock.Setup(r => r.GetByIdAsync(attemptId)).ReturnsAsync(attempt);

        FinishAttemptRequest request = new()
        {
            FinishedAt = DateTime.UtcNow,
            Answers = new List<AnswerSubmissionDto>
            {
                new() { QuestionId = questionId, Answer = new List<string> { "A", "B" } }
            }
        };

        SetupSessionHeader(sessionId);

        // Act
        IActionResult result = await _controller.Finish(attemptId, request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        int totalScore = GetTotalScore(okResult.Value);
        Assert.Equal(2, totalScore); // 2 правильных - 0 неправильных = 2
    }

    [Fact]
    public async Task Finish_MultipleChoice_PartialCorrect_ReturnsCorrectPoints()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();
        Guid questionId = Guid.NewGuid();

        Attempt attempt = new()
        {
            Id = attemptId,
            SessionId = sessionId,
            Student = new StudentSnapshot { FullName = "Test Student", GroupName = "Test Group" },
            Exam = new ExamSnapshot
            {
                Id = Guid.NewGuid(),
                Title = "Test Exam",
                Questions = new List<QuestionSnapshot>
                {
                    new()
                    {
                        Id = questionId,
                        Text = "Question 1",
                        Type = QuestionType.MultipleChoice,
                        Options = new List<string> { "A", "B", "C", "D" },
                        CorrectAnswers = new List<string> { "A", "B" }
                    }
                }
            }
        };

        _attemptRepositoryMock.Setup(r => r.GetByIdAsync(attemptId)).ReturnsAsync(attempt);

        FinishAttemptRequest request = new()
        {
            FinishedAt = DateTime.UtcNow,
            Answers = new List<AnswerSubmissionDto>
            {
                new() { QuestionId = questionId, Answer = new List<string> { "A", "C" } } // 1 правильный, 1 неправильный
            }
        };

        SetupSessionHeader(sessionId);

        // Act
        IActionResult result = await _controller.Finish(attemptId, request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        int totalScore = GetTotalScore(okResult.Value);
        Assert.Equal(0, totalScore); // 1 правильный - 1 неправильный = 0
    }

    [Fact]
    public async Task Finish_TextInput_CorrectAnswer_Returns3Points()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();
        Guid questionId = Guid.NewGuid();

        Attempt attempt = new()
        {
            Id = attemptId,
            SessionId = sessionId,
            Student = new StudentSnapshot { FullName = "Test Student", GroupName = "Test Group" },
            Exam = new ExamSnapshot
            {
                Id = Guid.NewGuid(),
                Title = "Test Exam",
                Questions = new List<QuestionSnapshot>
                {
                    new()
                    {
                        Id = questionId,
                        Text = "Question 1",
                        Type = QuestionType.TextInput,
                        Options = new List<string>(),
                        CorrectAnswers = new List<string> { "correct answer" }
                    }
                }
            }
        };

        _attemptRepositoryMock.Setup(r => r.GetByIdAsync(attemptId)).ReturnsAsync(attempt);

        FinishAttemptRequest request = new()
        {
            FinishedAt = DateTime.UtcNow,
            Answers = new List<AnswerSubmissionDto>
            {
                new() { QuestionId = questionId, Answer = "correct answer" }
            }
        };

        SetupSessionHeader(sessionId);

        // Act
        IActionResult result = await _controller.Finish(attemptId, request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        int totalScore = GetTotalScore(okResult.Value);
        Assert.Equal(3, totalScore);
    }

    [Fact]
    public async Task Finish_TextInput_CorrectAnswerWithWhitespace_Returns3Points()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();
        Guid questionId = Guid.NewGuid();

        Attempt attempt = new()
        {
            Id = attemptId,
            SessionId = sessionId,
            Student = new StudentSnapshot { FullName = "Test Student", GroupName = "Test Group" },
            Exam = new ExamSnapshot
            {
                Id = Guid.NewGuid(),
                Title = "Test Exam",
                Questions = new List<QuestionSnapshot>
                {
                    new()
                    {
                        Id = questionId,
                        Text = "Question 1",
                        Type = QuestionType.TextInput,
                        Options = new List<string>(),
                        CorrectAnswers = new List<string> { "correct answer" }
                    }
                }
            }
        };

        _attemptRepositoryMock.Setup(r => r.GetByIdAsync(attemptId)).ReturnsAsync(attempt);

        FinishAttemptRequest request = new()
        {
            FinishedAt = DateTime.UtcNow,
            Answers = new List<AnswerSubmissionDto>
            {
                new() { QuestionId = questionId, Answer = "  correct answer  " }
            }
        };

        SetupSessionHeader(sessionId);

        // Act
        IActionResult result = await _controller.Finish(attemptId, request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        int totalScore = GetTotalScore(okResult.Value);
        Assert.Equal(3, totalScore);
    }

    [Fact]
    public async Task Finish_NullAnswer_Returns0Points()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();
        Guid questionId = Guid.NewGuid();

        Attempt attempt = new()
        {
            Id = attemptId,
            SessionId = sessionId,
            Student = new StudentSnapshot { FullName = "Test Student", GroupName = "Test Group" },
            Exam = new ExamSnapshot
            {
                Id = Guid.NewGuid(),
                Title = "Test Exam",
                Questions = new List<QuestionSnapshot>
                {
                    new()
                    {
                        Id = questionId,
                        Text = "Question 1",
                        Type = QuestionType.SingleChoice,
                        Options = new List<string> { "A", "B" },
                        CorrectAnswers = new List<string> { "A" }
                    }
                }
            }
        };

        _attemptRepositoryMock.Setup(r => r.GetByIdAsync(attemptId)).ReturnsAsync(attempt);

        FinishAttemptRequest request = new()
        {
            FinishedAt = DateTime.UtcNow,
            Answers = new List<AnswerSubmissionDto>
            {
                new() { QuestionId = questionId, Answer = null }
            }
        };

        SetupSessionHeader(sessionId);

        // Act
        IActionResult result = await _controller.Finish(attemptId, request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        int totalScore = GetTotalScore(okResult.Value);
        Assert.Equal(0, totalScore);
    }

    [Fact]
    public async Task Finish_MultipleQuestions_CalculatesTotalCorrectly()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();
        Guid question1Id = Guid.NewGuid();
        Guid question2Id = Guid.NewGuid();
        Guid question3Id = Guid.NewGuid();

        Attempt attempt = new()
        {
            Id = attemptId,
            SessionId = sessionId,
            Student = new StudentSnapshot { FullName = "Test Student", GroupName = "Test Group" },
            Exam = new ExamSnapshot
            {
                Id = Guid.NewGuid(),
                Title = "Test Exam",
                Questions = new List<QuestionSnapshot>
                {
                    new()
                    {
                        Id = question1Id,
                        Text = "Single Choice",
                        Type = QuestionType.SingleChoice,
                        Options = new List<string> { "A", "B" },
                        CorrectAnswers = new List<string> { "A" }
                    },
                    new()
                    {
                        Id = question2Id,
                        Text = "Multiple Choice",
                        Type = QuestionType.MultipleChoice,
                        Options = new List<string> { "A", "B", "C" },
                        CorrectAnswers = new List<string> { "A", "B" }
                    },
                    new()
                    {
                        Id = question3Id,
                        Text = "Text Input",
                        Type = QuestionType.TextInput,
                        Options = new List<string>(),
                        CorrectAnswers = new List<string> { "answer" }
                    }
                }
            }
        };

        _attemptRepositoryMock.Setup(r => r.GetByIdAsync(attemptId)).ReturnsAsync(attempt);

        FinishAttemptRequest request = new()
        {
            FinishedAt = DateTime.UtcNow,
            Answers = new List<AnswerSubmissionDto>
            {
                new() { QuestionId = question1Id, Answer = "A" }, // 1 балл
                new() { QuestionId = question2Id, Answer = new List<string> { "A", "B" } }, // 2 балла
                new() { QuestionId = question3Id, Answer = "answer" } // 3 балла
            }
        };

        SetupSessionHeader(sessionId);

        // Act
        IActionResult result = await _controller.Finish(attemptId, request);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        int totalScore = GetTotalScore(okResult.Value);
        Assert.Equal(6, totalScore); // 1 + 2 + 3 = 6
    }

    [Fact]
    public async Task Finish_InvalidSessionId_ReturnsBadRequest()
    {
        // Arrange
        Guid attemptId = Guid.NewGuid();
        FinishAttemptRequest request = new()
        {
            FinishedAt = DateTime.UtcNow,
            Answers = new List<AnswerSubmissionDto>()
        };

        // Устанавливаем HttpContext без заголовка сессии
        SetupEmptySessionHeader();

        // Act
        IActionResult result = await _controller.Finish(attemptId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Finish_AttemptNotFound_ReturnsNotFound()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();

        _attemptRepositoryMock.Setup(r => r.GetByIdAsync(attemptId)).ReturnsAsync((Attempt?)null);

        FinishAttemptRequest request = new()
        {
            FinishedAt = DateTime.UtcNow,
            Answers = new List<AnswerSubmissionDto>()
        };

        SetupSessionHeader(sessionId);

        // Act
        IActionResult result = await _controller.Finish(attemptId, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Finish_SessionMismatch_ReturnsBadRequest()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid differentSessionId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();

        Attempt attempt = new()
        {
            Id = attemptId,
            SessionId = differentSessionId,
            Student = new StudentSnapshot { FullName = "Test Student", GroupName = "Test Group" },
            Exam = new ExamSnapshot
            {
                Id = Guid.NewGuid(),
                Title = "Test Exam",
                Questions = new List<QuestionSnapshot>()
            }
        };

        _attemptRepositoryMock.Setup(r => r.GetByIdAsync(attemptId)).ReturnsAsync(attempt);

        FinishAttemptRequest request = new()
        {
            FinishedAt = DateTime.UtcNow,
            Answers = new List<AnswerSubmissionDto>()
        };

        SetupSessionHeader(sessionId);

        // Act
        IActionResult result = await _controller.Finish(attemptId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    private void SetupSessionHeader(Guid sessionId)
    {
        DefaultHttpContext context = new();
        context.Request.Headers["X-Session-Id"] = sessionId.ToString();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
    }

    private void SetupEmptySessionHeader()
    {
        DefaultHttpContext context = new();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
    }
}
