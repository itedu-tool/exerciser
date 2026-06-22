using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Models;
using Exerciser.WebApi.Repositories;

namespace Exerciser.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly IGroupRepository _groupRepository;
    private readonly ISessionRepository _sessionRepository;

    public SessionsController(IGroupRepository groupRepository, ISessionRepository sessionRepository)
    {
        _groupRepository = groupRepository;
        _sessionRepository = sessionRepository;
    }

    /// <summary>
    /// Создать сессию для студента (логин).
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartSessionRequest request)
    {
        Group? group = await _groupRepository.GetByIdAsync(request.GroupId);
        if (group == null)
        {
            return BadRequest(new { error = "Группа не найдена" });
        }

        Student? student = group.Students.Find(s => s.Id == request.StudentId);
        if (student == null)
        {
            return BadRequest(new { error = "Студент не найден в группе" });
        }

        StudentSnapshot snapshot = new() { FullName = student.FullName, GroupName = group.Name };

        Session session = new() { Student = snapshot };
        await _sessionRepository.CreateAsync(session);

        return Ok(new StartSessionResponse { SessionId = session.Id });
    }
}