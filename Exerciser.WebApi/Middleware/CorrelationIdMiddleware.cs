using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Exerciser.WebApi.Middleware;

/// <summary>
/// Middleware для генерации и передачи CorrelationId.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = GetOrGenerateCorrelationId(context);
        context.TraceIdentifier = correlationId; // для совместимости с ASP.NET Core

        using (_logger.BeginScope(new { CorrelationId = correlationId }))
        {
            context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            await _next(context);
        }
    }

    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out StringValues headerValue) &&
            !string.IsNullOrEmpty(headerValue))
        {
            return headerValue.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}