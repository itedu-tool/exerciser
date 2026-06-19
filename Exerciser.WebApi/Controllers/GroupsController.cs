using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Models;
using Exerciser.WebApi.Repositories;

namespace Exerciser.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly IGroupRepository _groupRepository;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IGroupRepository groupRepository, ILogger<GroupsController> logger)
    {
        _groupRepository = groupRepository;
        _logger = logger;
    }

    /// <summary>
    /// Получить список всех групп со студентами.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        List<Group> groups = await _groupRepository.GetAllAsync();
        IEnumerable<GroupInfoDto> result = groups.Select(g => new GroupInfoDto
        {
            Id = g.Id.ToString(),
            Name = g.Name,
            Students = g.Students.Select(s => new StudentInfoDto { Id = s.Id.ToString(), FullName = s.FullName })
                .ToList()
        });
        return Ok(result);
    }

    /// <summary>
    /// Создать новую группу.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request)
    {
        Group group = new() { Name = request.Name };
        await _groupRepository.CreateAsync(group);
        return Created($"/api/v1/groups/{group.Id}",
            new GroupInfoDto { Id = group.Id.ToString(), Name = group.Name, Students = new List<StudentInfoDto>() });
    }

    /// <summary>
    /// Импорт группы из JSON-файла.
    /// </summary>
    [HttpPost("import")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "Файл не загружен" });
        }

        if (!Path.GetExtension(file.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Файл должен быть в формате JSON" });
        }

        using Stream stream = file.OpenReadStream();
        ImportGroupRequest? importData = await JsonSerializer.DeserializeAsync<ImportGroupRequest>(
            stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (importData == null || string.IsNullOrWhiteSpace(importData.Name))
        {
            return BadRequest(new { error = "Неверные данные группы: название обязательно" });
        }

        Group group = new()
        {
            Name = importData.Name,
            Students = importData.Students?.Select(s => new Student
            {
                LastName = s.LastName, FirstName = s.FirstName, Patronymic = s.Patronymic
            }).ToList() ?? new List<Student>()
        };

        await _groupRepository.CreateAsync(group);
        return Created($"/api/v1/groups/{group.Id}",
            new GroupInfoDto
            {
                Id = group.Id.ToString(),
                Name = group.Name,
                Students = group.Students.Select(s => new StudentInfoDto
                {
                    Id = s.Id.ToString(), FullName = s.FullName
                }).ToList()
            });
    }

    /// <summary>
    /// Добавить студента в группу.
    /// </summary>
    [HttpPost("{groupId:guid}/students")]
    public async Task<IActionResult> AddStudent(Guid groupId, [FromBody] AddStudentToGroupRequest request)
    {
        Group? group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            return NotFound(new { error = "Группа не найдена" });
        }

        Student student = new()
        {
            LastName = request.LastName, FirstName = request.FirstName, Patronymic = request.Patronymic
        };
        group.Students.Add(student);
        await _groupRepository.UpdateAsync(group);

        return Created($"/api/v1/groups/{groupId}/students/{student.Id}",
            new StudentInfoDto { Id = student.Id.ToString(), FullName = student.FullName });
    }
}