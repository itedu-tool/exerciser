using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Exerciser.WebApi.Metrics;

namespace Exerciser.WebApi.Middleware;

/// <summary>
/// Middleware для сбора метрик по HTTP-запросам.
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ExerciserMetrics _metrics;

    public MetricsMiddleware(RequestDelegate next, ExerciserMetrics metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
            stopwatch.Stop();

            var duration = stopwatch.Elapsed.TotalSeconds;
            var statusCode = context.Response.StatusCode;

            _metrics.RequestsTotal.Add(1,
                new KeyValuePair<string, object?>("method", context.Request.Method),
                new KeyValuePair<string, object?>("route", context.Request.Path),
                new KeyValuePair<string, object?>("status", statusCode.ToString()));

            _metrics.RequestDuration.Record(duration,
                new KeyValuePair<string, object?>("method", context.Request.Method),
                new KeyValuePair<string, object?>("route", context.Request.Path));

            if (statusCode >= 400 && statusCode < 600)
            {
                _metrics.ErrorsTotal.Add(1,
                    new KeyValuePair<string, object?>("status", statusCode.ToString()),
                    new KeyValuePair<string, object?>("method", context.Request.Method));
            }
        }
        catch (Exception)
        {
            stopwatch.Stop();
            _metrics.ErrorsTotal.Add(1,
                new KeyValuePair<string, object?>("status", "500"),
                new KeyValuePair<string, object?>("method", context.Request.Method));
            throw;
        }
    }
}