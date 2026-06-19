using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Models;
using Exerciser.WebApi.Repositories;

namespace Exerciser.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAttemptRepository _attemptRepository;

    public AnalyticsController(IAttemptRepository attemptRepository)
    {
        _attemptRepository = attemptRepository;
    }

    /// <summary>
    /// Получить последние завершённые попытки по каждому студенту и экзамену.
    /// </summary>
    [HttpGet("attempts/last")]
    public async Task<IActionResult> GetLastAttempts()
    {
        IEnumerable<Attempt> attempts = await _attemptRepository.GetLastFinishedAttemptsByStudentAndExamAsync();
        IEnumerable<AttemptAnalyticsDto> result = attempts.Select(a =>
        {
            int maxScore = a.Exam.Questions.Sum(q =>
                q.Type == "SingleChoice" ? 1 :
                q.Type == "MultipleChoice" ? q.CorrectAnswers.Count :
                3);
            int percent = maxScore > 0 ? (int)Math.Round((double)a.TotalScore / maxScore * 100) : 0;
            int durationMinutes = a.FinishedAt.HasValue && a.StartedAt != default
                ? (int)Math.Round((a.FinishedAt.Value - a.StartedAt).TotalMinutes)
                : 0;

            return new AttemptAnalyticsDto
            {
                AttemptId = a.Id,
                StudentFullName = a.Student.FullName,
                GroupName = a.Student.GroupName,
                ExamTitle = a.Exam.Title,
                TotalScore = a.TotalScore,
                MaxPossibleScore = maxScore,
                Percent = percent,
                FinishedAt = a.FinishedAt ?? a.StartedAt,
                DurationMinutes = durationMinutes
            };
        });

        return Ok(result);
    }
}