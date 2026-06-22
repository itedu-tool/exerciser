using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Models;
using Exerciser.WebApi.Repositories;

namespace Exerciser.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AttemptsController : ControllerBase
{
    private readonly IAttemptRepository _attemptRepository;
    private readonly IExamRepository _examRepository;
    private readonly ISessionRepository _sessionRepository;

    public AttemptsController(IAttemptRepository attemptRepository, IExamRepository examRepository,
        ISessionRepository sessionRepository)
    {
        _attemptRepository = attemptRepository;
        _examRepository = examRepository;
        _sessionRepository = sessionRepository;
    }

    private bool TryGetSessionId(out Guid sessionId)
    {
        if (Request.Headers.TryGetValue("X-Session-Id", out StringValues header) && Guid.TryParse(header, out Guid id))
        {
            sessionId = id;
            return true;
        }

        sessionId = Guid.Empty;
        return false;
    }

    /// <summary>
    /// Начать новую попытку прохождения экзамена (требуется X-Session-Id).
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartAttemptRequest request)
    {
        if (!TryGetSessionId(out Guid sessionId))
        {
            return BadRequest(new { error = "X-Session-Id header required" });
        }

        Session? session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            return BadRequest(new { error = "Invalid session" });
        }

        Exam? exam = await _examRepository.GetByIdAsync(request.ExamId);
        if (exam == null)
        {
            return NotFound(new { error = "Exam not found" });
        }

        Attempt? existing = await _attemptRepository.GetLatestUnfinishedAsync(sessionId, request.ExamId);
        if (existing != null)
        {
            return BadRequest(new { error = "Unfinished attempt already exists" });
        }

        List<Question> singleQuestions = exam.Questions.Where(q => q.Type == "SingleChoice").ToList();
        List<Question> multipleQuestions = exam.Questions.Where(q => q.Type == "MultipleChoice").ToList();
        List<Question> textQuestions = exam.Questions.Where(q => q.Type == "TextInput").ToList();

        int singleToTake = exam.SingleChoiceToShow > 0
            ? Math.Min(exam.SingleChoiceToShow, singleQuestions.Count)
            : singleQuestions.Count;
        int multipleToTake = exam.MultipleChoiceToShow > 0
            ? Math.Min(exam.MultipleChoiceToShow, multipleQuestions.Count)
            : multipleQuestions.Count;
        int textToTake = exam.TextInputToShow > 0
            ? Math.Min(exam.TextInputToShow, textQuestions.Count)
            : textQuestions.Count;

        List<Question> selected = new();
        selected.AddRange(singleQuestions.OrderBy(x => Guid.NewGuid()).Take(singleToTake));
        selected.AddRange(multipleQuestions.OrderBy(x => Guid.NewGuid()).Take(multipleToTake));
        selected.AddRange(textQuestions.OrderBy(x => Guid.NewGuid()).Take(textToTake));
        selected = selected.OrderBy(x => Guid.NewGuid()).ToList();

        ExamSnapshot examSnapshot = new()
        {
            Id = exam.Id,
            Title = exam.Title,
            Description = exam.Description,
            Questions = selected.Select(q => new QuestionSnapshot
            {
                Id = q.Id,
                Text = q.Text,
                Type = q.Type,
                Options = q.Options,
                CorrectAnswers = q.CorrectAnswers
            }).ToList()
        };

        Attempt attempt = new() { SessionId = sessionId, Student = session.Student, Exam = examSnapshot };
        await _attemptRepository.CreateAsync(attempt);

        ExamSnapshotDto examDto = new()
        {
            Id = examSnapshot.Id,
            Title = examSnapshot.Title,
            Description = examSnapshot.Description,
            Questions = examSnapshot.Questions.Select(q => new QuestionSnapshotDto
            {
                Id = q.Id,
                Text = q.Text,
                Type = q.Type,
                Options = q.Options,
                CorrectAnswers = q.CorrectAnswers
            }).ToList()
        };

        return Ok(new StartAttemptResponse { AttemptId = attempt.Id, Exam = examDto });
    }

    /// <summary>
    /// Завершить попытку и сохранить ответы (требуется X-Session-Id).
    /// </summary>
    [HttpPost("{id:guid}/finish")]
    public async Task<IActionResult> Finish(Guid id, [FromBody] FinishAttemptRequest request)
    {
        if (!TryGetSessionId(out Guid sessionId))
        {
            return BadRequest(new { error = "X-Session-Id header required" });
        }

        Attempt? attempt = await _attemptRepository.GetByIdAsync(id);
        if (attempt == null)
        {
            return NotFound(new { error = "Attempt not found" });
        }

        if (attempt.SessionId != sessionId)
        {
            return BadRequest(new { error = "Attempt does not belong to this session" });
        }

        if (attempt.FinishedAt != null)
        {
            return BadRequest(new { error = "Attempt already finished" });
        }

        List<StoredAnswer> storedAnswers = request.Answers.Select(a =>
        {
            object? answerValue = a.Answer switch
            {
                JsonElement json when json.ValueKind == JsonValueKind.String => json.GetString(),
                JsonElement json when json.ValueKind == JsonValueKind.Array => json.EnumerateArray()
                    .Select(e => e.GetString()).ToList(),
                JsonElement json when json.ValueKind == JsonValueKind.Null => null,
                JsonElement json => json.ToString(),
                _ => a.Answer
            };
            return new StoredAnswer { QuestionId = a.QuestionId, AnswerValue = answerValue, Score = a.Score };
        }).ToList();

        attempt.Answers = storedAnswers;
        attempt.FinishedAt = request.FinishedAt;
        attempt.TotalScore = request.TotalScore;
        await _attemptRepository.UpdateAsync(attempt);

        return Ok(new { success = true });
    }

    /// <summary>
    /// Получить результат завершённой попытки (требуется X-Session-Id).
    /// </summary>
    [HttpGet("{id:guid}/result")]
    public async Task<IActionResult> GetResult(Guid id)
    {
        if (!TryGetSessionId(out Guid sessionId))
        {
            return BadRequest(new { error = "X-Session-Id header required" });
        }

        Attempt? attempt = await _attemptRepository.GetByIdAsync(id);
        if (attempt == null)
        {
            return NotFound(new { error = "Attempt not found" });
        }

        if (attempt.SessionId != sessionId)
        {
            return BadRequest(new { error = "Access denied" });
        }

        int maxScore = attempt.Exam.Questions.Sum(q =>
            q.Type == "SingleChoice" ? 1 :
            q.Type == "MultipleChoice" ? q.CorrectAnswers.Count :
            3);

        List<QuestionResultDto> questionResults = attempt.Exam.Questions.Select(q =>
        {
            StoredAnswer? stored = attempt.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            int max = q.Type == "SingleChoice" ? 1 :
                q.Type == "MultipleChoice" ? q.CorrectAnswers.Count : 3;
            return new QuestionResultDto
            {
                Text = q.Text,
                Type = q.Type,
                Options = q.Options,
                CorrectAnswers = q.CorrectAnswers,
                UserAnswer = stored?.AnswerValue,
                Score = stored?.Score ?? 0,
                MaxScore = max
            };
        }).ToList();

        AttemptResultDto result = new()
        {
            AttemptId = attempt.Id,
            ExamTitle = attempt.Exam.Title,
            StudentFullName = attempt.Student.FullName,
            GroupName = attempt.Student.GroupName,
            StartedAt = attempt.StartedAt,
            FinishedAt = attempt.FinishedAt ?? attempt.StartedAt,
            TotalScore = attempt.TotalScore,
            MaxPossibleScore = maxScore,
            Questions = questionResults
        };
        return Ok(result);
    }
}