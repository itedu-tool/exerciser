using System.Diagnostics.Metrics;

namespace Exerciser.WebApi.Metrics;

/// <summary>
/// Класс для определения метрик приложения.
/// </summary>
public class ExerciserMetrics
{
    private readonly Meter _meter;

    /// <summary>Счётчик количества запросов.</summary>
    public Counter<int> RequestsTotal { get; }

    /// <summary>Гистограмма длительности запросов в секундах.</summary>
    public Histogram<double> RequestDuration { get; }

    /// <summary>Счётчик ошибок.</summary>
    public Counter<int> ErrorsTotal { get; }

    public ExerciserMetrics()
    {
        _meter = new Meter("Exerciser.WebApi", "1.0.0");

        RequestsTotal = _meter.CreateCounter<int>("http_requests_total",
            description: "Total number of HTTP requests");

        RequestDuration = _meter.CreateHistogram<double>("http_request_duration_seconds",
            "s",
            "Duration of HTTP requests in seconds");

        ErrorsTotal = _meter.CreateCounter<int>("http_errors_total",
            description: "Total number of HTTP errors (status >= 400)");
    }
}