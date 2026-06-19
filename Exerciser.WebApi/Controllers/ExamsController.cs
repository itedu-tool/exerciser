using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentValidation;
using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Models;
using Exerciser.WebApi.Repositories;
using Exerciser.WebApi.Extensions;

namespace Exerciser.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ExamsController : ControllerBase
{
    private readonly IExamRepository _examRepository;
    private readonly IValidator<ImportExamDto> _validator;
    private readonly ILogger<ExamsController> _logger;

    public ExamsController(IExamRepository examRepository, IValidator<ImportExamDto> validator, ILogger<ExamsController> logger)
    {
        _examRepository = examRepository;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Импорт экзамена из JSON-файла (v1).
    /// </summary>
    [HttpPost("import")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Файл не загружен" });

        if (!Path.GetExtension(file.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Файл должен быть в формате JSON" });

        const long maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
            return BadRequest(new
            {
                error = $"Файл слишком большой. Максимум: 10 MB, получено: {file.Length / (1024 * 1024)} MB"
            });

        try
        {
            await using var stream = file.OpenReadStream();
            var importData = await JsonSerializer.DeserializeAsync<ImportExamDto>(
                stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (importData == null)
                return BadRequest(new { error = "JSON не содержит данных" });

            await _validator.ValidateAndThrowAsync(importData);

            var exam = importData.ToExam();
            await _examRepository.CreateAsync(exam);

            _logger.LogInformation("Экзамен успешно импортирован: {ExamId} - {ExamTitle} ({QuestionsCount} вопросов)",
                exam.Id, exam.Title, exam.Questions.Count);

            return Created($"/api/v1/exams/{exam.Id}", new ExamImportResponseDto
            {
                Id = exam.Id.ToString(),
                Title = exam.Title,
                QuestionsCount = exam.Questions.Count
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Ошибка десериализации JSON в файле {FileName}", file.FileName);
            return BadRequest(new { error = "Неверный формат JSON: " + ex.Message });
        }
    }

    /// <summary>
    /// Получить список всех экзаменов (только метаданные).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var exams = await _examRepository.GetAllAsync();
        if (exams == null || exams.Count == 0)
            return Ok(new { message = "Нет доступных экзаменов. Загрузите первый экзамен через импорт." });

        var summaries = exams.Select(e => e.ToSummaryDto());
        return Ok(summaries);
    }

    /// <summary>
    /// Получить экзамен по ID (полная информация, включая вопросы).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var exam = await _examRepository.GetByIdAsync(id);
        if (exam == null)
            return NotFound(new { error = "Экзамен не найден" });

        return Ok(exam.ToDetailsDto());
    }

    /// <summary>
    /// Полное обновление экзамена.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ImportExamDto updatedExam)
    {
        var existing = await _examRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound(new { error = "Экзамен не найден" });

        await _validator.ValidateAndThrowAsync(updatedExam);

        var exam = updatedExam.ToExam();
        exam.Id = id;
        exam.CreatedAt = existing.CreatedAt;

        await _examRepository.UpdateAsync(exam);
        _logger.LogInformation("Экзамен {ExamId} обновлён", id);

        return Ok(new ExamImportResponseDto
        {
            Id = exam.Id.ToString(),
            Title = exam.Title,
            QuestionsCount = exam.Questions.Count
        });
    }

    /// <summary>
    /// Удалить экзамен по ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _examRepository.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Экзамен не найден" });

        _logger.LogInformation("Экзамен {ExamId} удалён", id);
        return NoContent();
    }
}