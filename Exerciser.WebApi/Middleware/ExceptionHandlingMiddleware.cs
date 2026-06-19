using System;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using FluentValidation;

using Exerciser.WebApi.Exceptions;

namespace Exerciser.WebApi.Middleware;

/// <summary>
/// Глобальный middleware для перехвата исключений и возврата стандартизированного JSON-ответа.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанное исключение при обработке запроса {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        (int statusCode, string errorMessage) = exception switch
        {
            ValidationException validationEx => (StatusCodes.Status400BadRequest, validationEx.Message),
            ImportValidationException importEx => (StatusCodes.Status400BadRequest, importEx.Message),
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера. Попробуйте позже.")
        };

        context.Response.StatusCode = statusCode;

        var response = new { error = errorMessage };
        string json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
}