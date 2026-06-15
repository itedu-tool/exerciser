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
        ILogger<MongoDbMigrationService> migrationLogger = sp.GetRequiredService<ILogger<MongoDbMigrationService>>();
        MongoDbSettings settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
        return new MongoDbMigrationService(database, migrationLogger, settings.ExamsCollectionName ?? "Exams");
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

    builder.Services.AddScoped<IGroupRepository, GroupRepository>();
    builder.Services.AddScoped<ISessionRepository, SessionRepository>();
    builder.Services.AddScoped<IAttemptRepository, AttemptRepository>();

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

    // GET / - Корневой эндпоинт API.
    app.MapGet("/", () => "Exerciser API запущен. Используйте /scalar/v1 для документации.")
        .WithName("Root")
        .WithSummary("Корневой эндпоинт API")
        .RequireRateLimiting("fixed");

    // GET /health - Проверка здоровья API (устаревший, без версии).
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
            });
        })
        .WithName("HealthCheckLegacy")
        .WithSummary("Проверка здоровья API (устаревший, без версии)")
        .Produces<HealthCheckResponseDto>(StatusCodes.Status200OK)
        .RequireRateLimiting("fixed");

    #endregion

    #region API v1 эндпоинты

    const string apiV1Prefix = "/api/v1";

    // GET /api/v1/health - Проверка здоровья API (v1).
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

    // POST /api/v1/exams/import - Импорт экзамена из JSON-файла (v1).
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

                if (importData == null)
                {
                    return Results.BadRequest(new { error = "JSON не содержит данных" });
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
                    Title = importData.Title,
                    Description = importData.Description,
                    CreatedAt = DateTime.UtcNow,
                    Questions = importData.Questions.Select(q => new Question
                    {
                        Text = q.Text,
                        Type = q.Type,
                        Options = q.Options ?? [],
                        CorrectAnswers = q.CorrectAnswers
                    }).ToList(),
                    SingleChoiceToShow = importData.SingleChoiceToShow,
                    MultipleChoiceToShow = importData.MultipleChoiceToShow,
                    TextInputToShow = importData.TextInputToShow
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

    // GET /api/v1/exams - Получить список всех экзаменов (только метаданные).
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
                SingleChoiceCount = e.Questions.Count(q => q.Type == "SingleChoice"),
                MultipleChoiceCount = e.Questions.Count(q => q.Type == "MultipleChoice"),
                TextInputCount = e.Questions.Count(q => q.Type == "TextInput"),
                SingleChoiceToShow = e.SingleChoiceToShow,
                MultipleChoiceToShow = e.MultipleChoiceToShow,
                TextInputToShow = e.TextInputToShow,
                CreatedAt = e.CreatedAt
            });

            return Results.Ok(examList);
        })
        .WithName("GetAllExams")
        .WithSummary("Получить список всех экзаменов")
        .WithDescription("Возвращает список экзаменов без вопросов, только метаданные и количество вопросов по типам.")
        .Produces<List<ExamSummaryDto>>(StatusCodes.Status200OK);

    // GET /api/v1/exams/{id} - Получить экзамен по ID (полная информация, включая вопросы).
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
                }).ToList(),
                SingleChoiceToShow = exam.SingleChoiceToShow,
                MultipleChoiceToShow = exam.MultipleChoiceToShow,
                TextInputToShow = exam.TextInputToShow
            });
        })
        .WithName("GetExamById")
        .WithSummary("Получить экзамен по ID")
        .WithDescription("Возвращает полную информацию об экзамене, включая вопросы и правильные ответы.")
        .Produces<ExamDetailsDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

    // PUT /api/v1/exams/{id} - Полное обновление экзамена.
    examsGroup.MapPut("/{id:guid}", async (
            Guid id,
            ImportExamDto updatedExam,
            IExamRepository repo,
            IExamImportValidator validator,
            ILogger<Program> updateLogger) =>
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
                }).ToList(),
                SingleChoiceToShow = updatedExam.SingleChoiceToShow,
                MultipleChoiceToShow = updatedExam.MultipleChoiceToShow,
                TextInputToShow = updatedExam.TextInputToShow
            };

            await repo.UpdateAsync(exam);
            updateLogger.LogInformation("Экзамен {ExamId} обновлён", id);

            return Results.Ok(new ExamImportResponseDto
            {
                Id = exam.Id.ToString(),
                Title = exam.Title,
                QuestionsCount = exam.Questions.Count
            });
        })
        .RequireRateLimiting("fixed")
        .WithName("UpdateExam")
        .WithSummary("Полное обновление экзамена")
        .WithDescription("Заменяет все поля экзамена (название, описание, вопросы) на переданные.")
        .Produces<ExamImportResponseDto>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

    // DELETE /api/v1/exams/{id} - Удалить экзамен по ID.
    examsGroup.MapDelete("/{id:guid}", async (Guid id, IExamRepository repo, ILogger<Program> deleteLogger) =>
        {
            bool deleted = await repo.DeleteAsync(id);
            if (!deleted)
            {
                return Results.NotFound(new { error = "Экзамен не найден" });
            }

            deleteLogger.LogInformation("Экзамен {ExamId} удалён", id);
            return Results.NoContent();
        })
        .WithName("DeleteExam")
        .WithSummary("Удалить экзамен по ID")
        .WithDescription("Удаляет экзамен из базы данных.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

    #endregion

    #region Группа эндпоинтов для работы с группами (Groups)

    RouteGroupBuilder groupsGroup = app.MapGroup($"{apiV1Prefix}/groups")
        .WithTags("Groups")
        .RequireRateLimiting("fixed");

    // GET /api/v1/groups - Получить список всех групп со студентами.
    groupsGroup.MapGet("/", async (IGroupRepository groupRepo) =>
        {
            var groups = await groupRepo.GetAllAsync();
            var result = groups.Select(g => new GroupInfoDto
            {
                Id = g.Id.ToString(),
                Name = g.Name,
                Students = g.Students
                    .Select(s => new StudentInfoDto { Id = s.Id.ToString(), FullName = s.FullName }).ToList()
            });
            return Results.Ok(result);
        })
        .WithName("GetGroups")
        .WithSummary("Получить список групп со студентами")
        .WithDescription("Возвращает все группы и вложенных студентов для выбора при входе.")
        .Produces<List<GroupInfoDto>>(StatusCodes.Status200OK);

    // POST /api/v1/groups - Создать новую группу.
    groupsGroup.MapPost("/", async (CreateGroupRequest request, IGroupRepository groupRepo) =>
        {
            var group = new Group { Name = request.Name };
            await groupRepo.CreateAsync(group);
            return Results.Created($"{apiV1Prefix}/groups/{group.Id}",
                new GroupInfoDto { Id = group.Id.ToString(), Name = group.Name, Students = [] });
        })
        .WithName("CreateGroup")
        .WithSummary("Создать новую группу")
        .Accepts<CreateGroupRequest>("application/json")
        .Produces<GroupInfoDto>(StatusCodes.Status201Created)
        .Produces<object>(StatusCodes.Status400BadRequest);

    // POST /api/v1/groups/import - Импорт группы из JSON-файла.
    groupsGroup.MapPost("/import", async (HttpRequest request, IGroupRepository groupRepo) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { error = "Expected multipart/form-data" });

            var file = request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { error = "File not provided" });

            if (!Path.GetExtension(file.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "File must be JSON" });

            using var stream = file.OpenReadStream();
            var importData = await JsonSerializer.DeserializeAsync<ImportGroupRequest>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (importData == null || string.IsNullOrWhiteSpace(importData.Name))
                return Results.BadRequest(new { error = "Invalid group data: name required" });

            var group = new Group
            {
                Name = importData.Name,
                Students = importData.Students?.Select(s => new Student
                {
                    LastName = s.LastName, FirstName = s.FirstName, Patronymic = s.Patronymic
                }).ToList() ?? []
            };

            await groupRepo.CreateAsync(group);

            return Results.Created($"{apiV1Prefix}/groups/{group.Id}",
                new GroupInfoDto
                {
                    Id = group.Id.ToString(),
                    Name = group.Name,
                    Students = group.Students.Select(s => new StudentInfoDto
                    {
                        Id = s.Id.ToString(), FullName = s.FullName
                    }).ToList()
                });
        })
        .DisableAntiforgery()
        .WithName("ImportGroup")
        .WithSummary("Импорт группы из JSON-файла")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<GroupInfoDto>(StatusCodes.Status201Created)
        .Produces<object>(StatusCodes.Status400BadRequest);

    // POST /api/v1/groups/{groupId}/students - Добавить студента в группу.
    groupsGroup.MapPost("/{groupId:guid}/students",
            async (Guid groupId, AddStudentToGroupRequest request, IGroupRepository groupRepo) =>
            {
                var group = await groupRepo.GetByIdAsync(groupId);
                if (group == null)
                    return Results.NotFound(new { error = "Group not found" });

                var student = new Student
                {
                    LastName = request.LastName, FirstName = request.FirstName, Patronymic = request.Patronymic
                };
                group.Students.Add(student);
                await groupRepo.UpdateAsync(group);

                return Results.Created($"{apiV1Prefix}/groups/{groupId}/students/{student.Id}",
                    new StudentInfoDto { Id = student.Id.ToString(), FullName = student.FullName });
            })
        .WithName("AddStudentToGroup")
        .WithSummary("Добавить студента в группу")
        .Accepts<AddStudentToGroupRequest>("application/json")
        .Produces<StudentInfoDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<object>(StatusCodes.Status400BadRequest);

    #endregion

    #region Эндпоинты попыток (Attempts)

    RouteGroupBuilder attemptsGroup = app.MapGroup($"{apiV1Prefix}/attempts")
        .RequireRateLimiting("fixed");

    // POST /api/v1/attempts/start - Начать новую попытку прохождения экзамена (требуется X-Session-Id).
    attemptsGroup.MapPost("/start", async (StartAttemptRequest request, HttpContext httpContext, IExamRepository examRepo, IAttemptRepository attemptRepo, ISessionRepository sessionRepo) =>
    {
        if (!httpContext.Request.Headers.TryGetValue("X-Session-Id", out var sessionIdHeader) || !Guid.TryParse(sessionIdHeader, out Guid sessionId))
            return Results.BadRequest(new { error = "X-Session-Id header required" });

        var session = await sessionRepo.GetByIdAsync(sessionId);
        if (session == null)
            return Results.BadRequest(new { error = "Invalid session" });

        var exam = await examRepo.GetByIdAsync(request.ExamId);
        if (exam == null)
            return Results.NotFound(new { error = "Exam not found" });

        var existing = await attemptRepo.GetLatestUnfinishedAsync(sessionId, request.ExamId);
        if (existing != null)
            return Results.BadRequest(new { error = "Unfinished attempt already exists" });

        int questionsToTakeSingle = exam.SingleChoiceToShow;
        int questionsToTakeMultiple = exam.MultipleChoiceToShow;
        int questionsToTakeText = exam.TextInputToShow;

        var singleQuestions = exam.Questions.Where(q => q.Type == "SingleChoice").ToList();
        var multipleQuestions = exam.Questions.Where(q => q.Type == "MultipleChoice").ToList();
        var textQuestions = exam.Questions.Where(q => q.Type == "TextInput").ToList();

        if (questionsToTakeSingle <= 0 || questionsToTakeSingle > singleQuestions.Count)
            questionsToTakeSingle = singleQuestions.Count;
        if (questionsToTakeMultiple <= 0 || questionsToTakeMultiple > multipleQuestions.Count)
            questionsToTakeMultiple = multipleQuestions.Count;
        if (questionsToTakeText <= 0 || questionsToTakeText > textQuestions.Count)
            questionsToTakeText = textQuestions.Count;

        var selectedSingle = singleQuestions.OrderBy(x => Guid.NewGuid()).Take(questionsToTakeSingle);
        var selectedMultiple = multipleQuestions.OrderBy(x => Guid.NewGuid()).Take(questionsToTakeMultiple);
        var selectedText = textQuestions.OrderBy(x => Guid.NewGuid()).Take(questionsToTakeText);

        var finalShuffled = selectedSingle.Concat(selectedMultiple).Concat(selectedText)
            .OrderBy(x => Guid.NewGuid())
            .ToList();

        var examSnapshot = new ExamSnapshot
        {
            Id = exam.Id,
            Title = exam.Title,
            Description = exam.Description,
            Questions = finalShuffled.Select(q => new QuestionSnapshot
            {
                Id = q.Id,
                Text = q.Text,
                Type = q.Type,
                Options = q.Options,
                CorrectAnswers = q.CorrectAnswers
            }).ToList()
        };

        var attempt = new Attempt
        {
            SessionId = sessionId,
            Student = session.Student,
            Exam = examSnapshot
        };
        await attemptRepo.CreateAsync(attempt);

        var examDto = new ExamSnapshotDto
        {
            Id = examSnapshot.Id,
            Title = examSnapshot.Title,
            Description = examSnapshot.Description,
            Questions = examSnapshot.Questions.Select(q => new QuestionSnapshotDto
            {
                Id = q.Id,
                Text = q.Text,
                Type = q.Type,
                Options = q.Options,
                CorrectAnswers = q.CorrectAnswers
            }).ToList()
        };

        return Results.Ok(new StartAttemptResponse
        {
            AttemptId = attempt.Id,
            Exam = examDto
        });
    })
    .RequireRateLimiting("fixed")
    .WithName("StartAttempt")
    .WithSummary("Начать новую попытку прохождения экзамена")
    .WithDescription("Создаёт новую попытку для указанного экзамена. Требуется заголовок X-Session-Id. Количество вопросов определяется полями SingleChoiceToShow, MultipleChoiceToShow, TextInputToShow экзамена.")
    .Accepts<StartAttemptRequest>("application/json")
    .Produces<StartAttemptResponse>(StatusCodes.Status200OK)
    .Produces<object>(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound);

    // POST /api/v1/attempts/{id}/finish - Завершить попытку и сохранить ответы (требуется X-Session-Id).
attemptsGroup.MapPost($"/{{id:guid}}/finish",
        async (Guid id, FinishAttemptRequest request, HttpContext httpContext, IAttemptRepository attemptRepo) =>
        {
            if (!httpContext.Request.Headers.TryGetValue("X-Session-Id", out var sessionIdHeader) ||
                !Guid.TryParse(sessionIdHeader, out Guid sessionId))
                return Results.BadRequest(new { error = "X-Session-Id header required" });

            var attempt = await attemptRepo.GetByIdAsync(id);
            if (attempt == null)
                return Results.NotFound(new { error = "Attempt not found" });

            if (attempt.SessionId != sessionId)
                return Results.BadRequest(new { error = "Attempt does not belong to this session" });

            if (attempt.FinishedAt != null)
                return Results.BadRequest(new { error = "Attempt already finished" });

            // Преобразование JsonElement в примитивные типы
            var storedAnswers = request.Answers.Select(a =>
            {
                object? answerValue;
                if (a.Answer is JsonElement jsonElement)
                {
                    answerValue = jsonElement.ValueKind switch
                    {
                        JsonValueKind.String => jsonElement.GetString(),
                        JsonValueKind.Array => jsonElement.EnumerateArray().Select(e => e.GetString()).ToList(),
                        JsonValueKind.Null => null,
                        _ => jsonElement.ToString()
                    };
                }
                else
                {
                    answerValue = a.Answer;
                }

                return new StoredAnswer
                {
                    QuestionId = a.QuestionId,
                    AnswerValue = answerValue,
                    Score = a.Score
                };
            }).ToList();

            attempt.Answers = storedAnswers;
            attempt.FinishedAt = request.FinishedAt;
            attempt.TotalScore = request.TotalScore;
            await attemptRepo.UpdateAsync(attempt);

            return Results.Ok(new { success = true });
        })
    .WithName("FinishAttempt")
    .WithSummary("Завершить попытку и сохранить ответы")
    .WithDescription("Принимает все ответы студента и итоговый балл. Требуется заголовок X-Session-Id.")
    .Accepts<FinishAttemptRequest>("application/json")
    .Produces<object>(StatusCodes.Status200OK)
    .Produces<object>(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound);

    // GET /api/v1/attempts/{id}/result - Получить результат завершённой попытки (требуется X-Session-Id).
    attemptsGroup.MapGet($"/{{id:guid}}/result",
            async (Guid id, HttpContext httpContext, IAttemptRepository attemptRepo) =>
            {
                if (!httpContext.Request.Headers.TryGetValue("X-Session-Id", out var sessionIdHeader) ||
                    !Guid.TryParse(sessionIdHeader, out Guid sessionId))
                    return Results.BadRequest(new { error = "X-Session-Id header required" });

                var attempt = await attemptRepo.GetByIdAsync(id);
                if (attempt == null)
                    return Results.NotFound(new { error = "Attempt not found" });

                if (attempt.SessionId != sessionId)
                    return Results.BadRequest(new { error = "Access denied" });

                var maxPossibleScore = attempt.Exam.Questions.Sum(q =>
                    q.Type == "SingleChoice" ? 1 :
                    q.Type == "MultipleChoice" ? q.CorrectAnswers.Count :
                    3);

                var questionResults = attempt.Exam.Questions.Select(q =>
                {
                    var storedAnswer = attempt.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
                    int maxScore = q.Type == "SingleChoice"
                        ? 1
                        : (q.Type == "MultipleChoice" ? q.CorrectAnswers.Count : 3);
                    return new QuestionResultDto
                    {
                        Text = q.Text,
                        Type = q.Type,
                        Options = q.Options,
                        CorrectAnswers = q.CorrectAnswers,
                        UserAnswer = storedAnswer?.AnswerValue,
                        Score = storedAnswer?.Score ?? 0,
                        MaxScore = maxScore
                    };
                }).ToList();

                var result = new AttemptResultDto
                {
                    AttemptId = attempt.Id,
                    ExamTitle = attempt.Exam.Title,
                    StudentFullName = attempt.Student.FullName,
                    GroupName = attempt.Student.GroupName,
                    StartedAt = attempt.StartedAt,
                    FinishedAt = attempt.FinishedAt ?? attempt.StartedAt,
                    TotalScore = attempt.TotalScore,
                    MaxPossibleScore = maxPossibleScore,
                    Questions = questionResults
                };
                return Results.Ok(result);
            })
        .WithName("GetAttemptResult")
        .WithSummary("Получить результат завершённой попытки")
        .WithDescription(
            "Возвращает детальную информацию о попытке: ответы, баллы, время. Требуется заголовок X-Session-Id.")
        .Produces<AttemptResultDto>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

    #endregion

    #region Эндпоинты сессий (Session)

    // POST /api/v1/sessions/start - Создать сессию для студента (логин).
    app.MapPost($"{apiV1Prefix}/sessions/start",
            async (StartSessionRequest request, IGroupRepository groupRepo, ISessionRepository sessionRepo) =>
            {
                var group = await groupRepo.GetByIdAsync(request.GroupId);
                if (group == null)
                    return Results.BadRequest(new { error = "Group not found" });

                var student = group.Students.FirstOrDefault(s => s.Id == request.StudentId);
                if (student == null)
                    return Results.BadRequest(new { error = "Student not found in group" });

                var studentSnapshot = new StudentSnapshot { FullName = student.FullName, GroupName = group.Name };

                var session = new Session { Student = studentSnapshot };
                await sessionRepo.CreateAsync(session);
                return Results.Ok(new StartSessionResponse { SessionId = session.Id });
            })
        .RequireRateLimiting("fixed")
        .WithName("StartSession")
        .WithSummary("Создать сессию для студента")
        .WithDescription("Выбирает группу и студента, возвращает SessionId dla последующих запросов.")
        .Accepts<StartSessionRequest>("application/json")
        .Produces<StartSessionResponse>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest);

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
        IMongoDatabase adminDb = mongoClient.GetDatabase("admin");
        BsonDocument command = BsonDocument.Parse("{ ping: 1 }");
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