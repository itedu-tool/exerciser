using System;
using Microsoft.AspNetCore.Mvc;
using Exerciser.WebApi.DTOs;

namespace Exerciser.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Проверка здоровья API (v1).
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var nowLocal = DateTime.Now;
        var utcNow = DateTime.UtcNow;
        var timeZone = TimeZoneInfo.Local;

        return Ok(new HealthCheckResponseDto
        {
            Status = "healthy",
            Timestamp = nowLocal,
            TimestampUtc = utcNow,
            TimeZone = timeZone.DisplayName,
            Offset = timeZone.GetUtcOffset(utcNow).ToString(),
            ApiVersion = "v1"
        });
    }
}