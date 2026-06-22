using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

using Scalar.AspNetCore;

using NLog.Web;

using System;
using System.Threading.Tasks;

using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Extensions;
using Exerciser.WebApi.Models;
using Exerciser.WebApi.Repositories;
using Exerciser.WebApi.Services;
using Exerciser.WebApi.Middleware;
using Exerciser.WebApi.Metrics;

using FluentValidation;
using FluentValidation.AspNetCore;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

using NLog.Extensions.Logging;

using StackExchange.Redis;

using ServiceCollectionExtensions = Exerciser.WebApi.Extensions.ServiceCollectionExtensions;

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    #region Регистрация сервисов

    builder.Services.AddControllers();

    string? corsOrigins = builder.Configuration["CORS:AllowedOrigins"];
    string[] allowedOrigins = ServiceCollectionExtensions.ParseCorsOrigins(corsOrigins);
    ILoggerFactory loggerFactory = LoggerFactory.Create(logging => logging.AddConsole().AddNLog());
    ILogger<Program> logger = loggerFactory.CreateLogger<Program>();
    logger.LogInformation("Настроены разрешённые источники CORS: {Origins}", string.Join(", ", allowedOrigins));
    builder.Services.AddCorsPolicy(allowedOrigins);

    builder.Services.AddOpenApiMetadata();
    builder.Services.Configure<ApiMetadata>(builder.Configuration.GetSection("ApiMetadata"));

    builder.Services.AddRateLimitingPolicies();

    builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
    builder.Services.AddMongoDb(logger);
    builder.Services.AddScoped<IMongoDatabase>(sp =>
    {
        MongoDbSettings settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
        IMongoClient client = sp.GetRequiredService<IMongoClient>();
        return client.GetDatabase(settings.DatabaseName ?? "exerciser_db");
    });
    builder.Services.AddScoped<IExamRepository>(sp =>
    {
        IMongoDatabase db = sp.GetRequiredService<IMongoDatabase>();
        MongoDbSettings settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
        return new ExamRepository(db, settings.ExamsCollectionName ?? "Exams");
    });
    builder.Services.AddScoped<IGroupRepository, GroupRepository>();
    builder.Services.AddScoped<ISessionRepository, SessionRepository>();
    builder.Services.AddScoped<IAttemptRepository, AttemptRepository>();

    #region Redis и кеширование

    string? redisConnection = builder.Configuration["Redis:ConnectionString"];
    IConnectionMultiplexer? multiplexer = null;
    if (!string.IsNullOrEmpty(redisConnection))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "Exerciser_";
        });
        multiplexer = ConnectionMultiplexer.Connect(redisConnection);
        builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        logger.LogInformation("Настроено кеширование Redis");
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
        logger.LogInformation("Настроено кеширование MemoryCache (Redis недоступен)");
    }

    builder.Services.AddScoped<ICacheService>(sp =>
    {
        IDistributedCache cache = sp.GetRequiredService<IDistributedCache>();
        ILogger<DistributedCacheService> log = sp.GetRequiredService<ILogger<DistributedCacheService>>();
        IConnectionMultiplexer? multiplexer = sp.GetService<IConnectionMultiplexer>();
        return new DistributedCacheService(cache, log, multiplexer);
    });

    #endregion

    builder.Services.AddScoped<IMongoDbMigrationService, MongoDbMigrationService>();

    #region FluentValidation

    builder.Services.AddFluentValidationAutoValidation();
    builder.Services
        .AddValidatorsFromAssemblyContaining<Exerciser.WebApi.Validators.FluentValidation.ImportExamDtoValidator>();

    #endregion

    #region Метрики

    builder.Services.AddSingleton<ExerciserMetrics>();

    #endregion

    #endregion

    WebApplication app = builder.Build();

    #region Инициализация базы данных

    using (IServiceScope scope = app.Services.CreateScope())
    {
        IMongoDbMigrationService migration = scope.ServiceProvider.GetRequiredService<IMongoDbMigrationService>();
        await migration.InitializeAsync();
    }

    await CheckMongodbConnection(app.Services, logger);

    #endregion

    #region Настройка HTTP и OpenAPI

    string httpPort = builder.Configuration["ASPNETCORE_HTTP_PORT"] ?? "8080";
    app.Urls.Clear();
    app.Urls.Add($"http://0.0.0.0:{httpPort}");

    app.MapOpenApi();
    ApiMetadata? apiMetadata = app.Services.GetRequiredService<IOptions<ApiMetadata>>().Value;
    app.MapScalarApiReference(options =>
    {
        options.WithTitle(apiMetadata?.Title ?? "Exerciser API")
            .WithTheme(ScalarTheme.Alternate)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    #endregion

    #region Middleware и маршрутизация

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<MetricsMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseRateLimiter();
    app.UseCors("AllowConfiguredOrigins");
    app.MapControllers();

    #endregion

    #region Legacy endpoints (без версии)

    app.MapGet("/", () => "Exerciser API запущен. Используйте /scalar/v1 для документации.")
        .RequireRateLimiting("fixed");
    app.MapGet("/health", async () =>
    {
        DateTime nowLocal = DateTime.Now;
        DateTime utcNow = DateTime.UtcNow;
        TimeZoneInfo timeZone = TimeZoneInfo.Local;
        return Results.Ok(new HealthCheckResponseDto
        {
            Status = "healthy",
            Timestamp = nowLocal,
            TimestampUtc = utcNow,
            TimeZone = timeZone.DisplayName,
            Offset = timeZone.GetUtcOffset(utcNow).ToString()
        });
    }).RequireRateLimiting("fixed");

    #endregion

    await app.RunAsync();
}
catch (Exception ex)
{
    // Логирование через статический NLog допустимо только здесь, в глобальном перехватчике.
    // Альтернатива – использовать ILogger, но он недоступен в этом контексте.
    NLog.LogManager.GetCurrentClassLogger().Fatal(ex, "Приложение завершило работу неожиданно");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}

async Task CheckMongodbConnection(IServiceProvider services, ILogger<Program> logger)
{
    try
    {
        IMongoClient client = services.GetRequiredService<IMongoClient>();
        IMongoDatabase? adminDb = client.GetDatabase("admin");
        BsonDocument? command = BsonDocument.Parse("{ ping: 1 }");
        await adminDb.RunCommandAsync<BsonDocument>(command);
        logger.LogInformation("Подключение к MongoDB успешно проверено");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Не удалось подключиться к MongoDB. Проверьте строку подключения и учётные данные.");
        throw;
    }
}