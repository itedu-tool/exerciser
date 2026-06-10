using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

using Scalar.AspNetCore;

using NLog;
using NLog.Web;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Exerciser.WebApi.Models;
using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Repositories;
using Exerciser.WebApi.Services;
using Exerciser.WebApi.Validators;
using Exerciser.WebApi.Exceptions;
using Exerciser.WebApi.Extensions;

using Microsoft.AspNetCore.Routing;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

using ILogger = NLog.ILogger;

#region Настройка MongoDB GUID сериализации

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

#endregion

ILogger logger = LogManager
    .Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    #region CORS

    string? corsOriginsString = builder.Configuration["CORS:AllowedOrigins"];
    string[] corsAllowedOrigins = ServiceCollectionExtensions.ParseCorsOrigins(corsOriginsString);
    logger.Info($"Настроены разрешённые источники CORS: {string.Join(", ", corsAllowedOrigins)}");
    builder.Services.AddCorsPolicy(corsAllowedOrigins);

    #endregion

    #region OpenAPI

    builder.Services.AddOpenApiMetadata();
    builder.Services.Configure<ApiMetadata>(builder.Configuration.GetSection("ApiMetadata"));

    #endregion

    #region Rate limiting

    builder.Services.AddRateLimitingPolicies();

    #endregion

    #region MongoDB

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
        IMongoDatabase database = sp.GetRequiredService<IMongoDatabase>();
        MongoDbSettings settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
        string collectionName = settings.ExamsCollectionName ?? "Exams";
        return new ExamRepository(database, collectionName);
    });

    builder.Services.AddScoped<IMongoDbMigrationService>(sp =>
    {
        IMongoDatabase database = sp.GetRequiredService<IMongoDatabase>();
        ILogger<MongoDbMigrationService> logger = sp.GetRequiredService<ILogger<MongoDbMigrationService>>();
        MongoDbSettings settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
        return new MongoDbMigrationService(database, logger, settings.ExamsCollectionName ?? "Exams");
    });

    #endregion

    #region Кеширование (Redis / MemoryCache)

    string? redisConnection = builder.Configuration["Redis:ConnectionString"];
    if (!string.IsNullOrEmpty(redisConnection))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "Exerciser_";
        });
        logger.Info("✓ Настроено кеширование Redis");
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
        logger.Info("✓ Настроено кеширование MemoryCache (Redis недоступен)");
    }

    #endregion

    #region Сервисы приложения

    builder.Services.AddScoped<IExamImportValidator, ExamImportValidator>();
    builder.Services.AddScoped<ICacheService, DistributedCacheService>();

    #endregion

    #region Сборка приложения

    WebApplication app = builder.Build();

    #endregion

    #region Инициализация базы данных

    using (IServiceScope scope = app.Services.CreateScope())
    {
        IMongoDbMigrationService migrationService =
            scope.ServiceProvider.GetRequiredService<IMongoDbMigrationService>();
        await migrationService.InitializeAsync();
    }

    await CheckMongodbConnection(app.Services, logger);

    #endregion

    #region Настройка HTTP (порты)

    string httpPort = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORT") ?? "8080";
    app.Urls.Clear();
    app.Urls.Add($"http://0.0.0.0:{httpPort}");

    #endregion

    #region OpenAPI и Scalar UI

    app.MapOpenApi();

    ApiMetadata? apiMetadata = app.Services.GetRequiredService<IOptions<ApiMetadata>>().Value;
    app.MapScalarApiReference(options =>
    {
        options.WithTitle(apiMetadata?.Title ?? "Exerciser API")
            .WithTheme(ScalarTheme.Alternate)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    #endregion

    #region Middleware

    app.UseRateLimiter();
    app.UseCors("AllowConfiguredOrigins");

    #endregion

    #region Эндпоинты (без версии)

    app.MapGet("/", () => "Exerciser API запущен. Используйте /scalar/v1 для документации.")
        .WithName("Root")
        .WithSummary("Корневой эндпоинт API")
        .RequireRateLimiting("fixed");

    app.MapGet("/health", () =>
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
                // ApiVersion не задаётся (остаётся null)
            });
        })
        .WithName("HealthCheckLegacy")
        .WithSummary("Проверка здоровья API (устаревший, без версии)")
        .Produces<HealthCheckResponseDto>(StatusCodes.Status200OK)
        .RequireRateLimiting("fixed");

    #endregion

    #region API v1 эндпоинты

    const string apiV1Prefix = "/api/v1";

    app.MapGet($"{apiV1Prefix}/health", () =>
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
                Offset = timeZone.GetUtcOffset(utcNow).ToString(),
                ApiVersion = "v1"
            });
        })
        .WithName("HealthCheckV1")
        .WithSummary("Проверка здоровья API (v1)")
        .WithGroupName("v1")
        .Produces<HealthCheckResponseDto>(StatusCodes.Status200OK)
        .RequireRateLimiting("fixed");

    #region Группа эндпоинтов для работы с экзаменами

    RouteGroupBuilder examsGroup = app.MapGroup($"{apiV1Prefix}/exams")
        .WithTags("Exams")
        .RequireRateLimiting("fixed");

    // Импорт экзамена (POST /api/v1/exams/import)
    examsGroup.MapPost("/import", async (
            IFormFile file,
            IExamRepository examRepository,
            IExamImportValidator validator,
            ILogger<Program> importLogger) =>
        {
            if (file.Length == 0)
            {
                return Results.BadRequest(new { error = "Файл не загружен" });
            }

            if (!Path.GetExtension(file.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = "Файл должен быть в формате JSON" });
            }

            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return Results.BadRequest(new
                {
                    error = $"Файл слишком большой. Максимум: 10 MB, получено: {file.Length / (1024 * 1024)} MB"
                });
            }

            try
            {
                await using Stream stream = file.OpenReadStream();
                ImportExamDto? importData;
                try
                {
                    importData = await JsonSerializer.DeserializeAsync<ImportExamDto>(
                        stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    importLogger.LogWarning(ex, "Ошибка десериализации JSON в файле {FileName}", file.FileName);
                    return Results.BadRequest(new { error = "Неверный формат JSON: " + ex.Message });
                }

                try
                {
                    await validator.ValidateAsync(importData);
                }
                catch (ImportValidationException ex)
                {
                    importLogger.LogWarning(ex, "Ошибка валидации при импорте экзамена");
                    return Results.BadRequest(new { error = ex.Message });
                }

                Exam exam = new()
                {
                    Title = importData!.Title,
                    Description = importData.Description,
                    CreatedAt = DateTime.UtcNow,
                    Questions = importData.Questions.Select(q => new Question
                    {
                        Text = q.Text,
                        Type = q.Type,
                        Options = q.Options ?? [],
                        CorrectAnswers = q.CorrectAnswers
                    }).ToList()
                };

                await examRepository.CreateAsync(exam);

                importLogger.LogInformation(
                    "Экзамен успешно импортирован: {ExamId} - {ExamTitle} ({QuestionsCount} вопросов)",
                    exam.Id, exam.Title, exam.Questions.Count);

                return Results.Created($"{apiV1Prefix}/exams/{exam.Id}",
                    new ExamImportResponseDto
                    {
                        Id = exam.Id.ToString(), Title = exam.Title, QuestionsCount = exam.Questions.Count
                    });
            }
            catch (Exception ex)
            {
                importLogger.LogError(ex, "Непредвиденная ошибка при импорте экзамена");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .DisableAntiforgery()
        .WithName("ImportExamV1")
        .WithSummary("Импорт экзамена из JSON-файла (v1)")
        .WithDescription("Загружает JSON-файл со списком вопросов и правильными ответами (максимум 10 MB)")
        .Produces<ExamImportResponseDto>(StatusCodes.Status201Created)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .Produces<object>(StatusCodes.Status429TooManyRequests)
        .RequireRateLimiting("import-sliding");

    // GET /api/v1/exams - список экзаменов (только метаданные)
    examsGroup.MapGet("/", async (IExamRepository repo) =>
        {
            List<Exam>? exams = await repo.GetAllAsync();
            if (exams == null || exams.Count == 0)
            {
                return Results.Ok(new { message = "Нет доступных экзаменов. Загрузите первый экзамен через импорт." });
            }

            IEnumerable<ExamSummaryDto> examList = exams.Select(e => new ExamSummaryDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                QuestionsCount = e.Questions.Count,
                CreatedAt = e.CreatedAt
            });

            return Results.Ok(examList);
        })
        .WithName("GetAllExams")
        .WithSummary("Получить список всех экзаменов")
        .WithDescription("Возвращает список экзаменов без вопросов, только метаданные.")
        .Produces<List<ExamSummaryDto>>(StatusCodes.Status200OK);

    // GET /api/v1/exams/{id} - полный экзамен (с вопросами и ответами)
    examsGroup.MapGet("/{id}", async (string id, IExamRepository repo) =>
        {
            if (!Guid.TryParse(id, out Guid examId))
            {
                return Results.BadRequest(new { error = "Неверный формат идентификатора экзамена" });
            }

            Exam? exam = await repo.GetByIdAsync(examId);
            if (exam == null)
            {
                return Results.NotFound(new { error = "Экзамен не найден" });
            }

            return Results.Ok(new ExamDetailsDto
            {
                Id = exam.Id,
                Title = exam.Title,
                Description = exam.Description,
                CreatedAt = exam.CreatedAt,
                Questions = exam.Questions.Select(q => new QuestionDetailsDto
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = q.Type,
                    Options = q.Options,
                    CorrectAnswers = q.CorrectAnswers
                }).ToList()
            });
        })
        .WithName("GetExamById")
        .WithSummary("Получить экзамен по ID")
        .WithDescription("Возвращает полную информацию об экзамене, включая вопросы и правильные ответы.")
        .Produces<ExamDetailsDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    
    //PUT /api/v1/exams/:id
    examsGroup.MapPut("/{id:guid}", async (
            Guid id,
            ImportExamDto updatedExam,
            IExamRepository repo,
            IExamImportValidator validator,
            ILogger<Program> logger) =>
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing == null)
                return Results.NotFound(new { error = "Экзамен не найден" });
            
            try
            {
                await validator.ValidateAsync(updatedExam);
            }
            catch (ImportValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            
            var exam = new Exam
            {
                Id = id,
                Title = updatedExam.Title,
                Description = updatedExam.Description,
                CreatedAt = existing.CreatedAt,
                Questions = updatedExam.Questions.Select(q => new Question
                {
                    Text = q.Text,
                    Type = q.Type,
                    Options = q.Options ?? [],
                    CorrectAnswers = q.CorrectAnswers
                }).ToList()
            };

            await repo.UpdateAsync(exam);
            logger.LogInformation("Экзамен {ExamId} обновлён", id);

            return Results.Ok(new ExamImportResponseDto
            {
                Id = exam.Id.ToString(),
                Title = exam.Title,
                QuestionsCount = exam.Questions.Count
            });
        })
        .RequireRateLimiting("fixed")   // или отдельный лимит
        .WithName("UpdateExam")
        .WithSummary("Полное обновление экзамена");

    // DELETE /api/v1/exams/{id} - удаление экзамена
    examsGroup.MapDelete("/{id:guid}", async (Guid id, IExamRepository repo, ILogger<Program> logger) =>
        {
            bool deleted = await repo.DeleteAsync(id);
            if (!deleted)
            {
                return Results.NotFound(new { error = "Экзамен не найден" });
            }

            logger.LogInformation("Экзамен {ExamId} удалён", id);
            return Results.NoContent();
        })
        .WithName("DeleteExam")
        .WithSummary("Удалить экзамен по ID")
        .WithDescription("Удаляет экзамен из базы данных.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

    #endregion

    #endregion

    #region Запуск приложения

    await app.RunAsync();

    #endregion
}
catch (Exception ex)
{
    logger.Fatal(ex, "Приложение завершило работу неожиданно");
    throw;
}
finally
{
    LogManager.Shutdown();
}

return;

#region Вспомогательная функция: проверка подключения к MongoDB

async Task CheckMongodbConnection(IServiceProvider services, ILogger logger)
{
    try
    {
        IMongoClient mongoClient = services.GetRequiredService<IMongoClient>();
        IMongoDatabase? adminDb = mongoClient.GetDatabase("admin");
        BsonDocument? command = BsonDocument.Parse("{ ping: 1 }");
        await adminDb.RunCommandAsync<BsonDocument>(command);
        logger.Info("✓ Подключение к MongoDB успешно проверено");
    }
    catch (Exception ex)
    {
        logger.Error(ex, "✗ Не удалось подключиться к MongoDB. Проверьте строку подключения и учётные данные.");
        throw;
    }
}

#endregion