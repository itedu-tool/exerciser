using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Scalar.AspNetCore;
using NLog;
using NLog.Web;
using System;
using System.Threading.Tasks;

using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Extensions;
using Exerciser.WebApi.Models;
using Exerciser.WebApi.Repositories;
using Exerciser.WebApi.Services;
using Exerciser.WebApi.Validators;
using Exerciser.WebApi.Middleware;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    #region Регистрация сервисов

    builder.Services.AddControllers();

    var corsOrigins = builder.Configuration["CORS:AllowedOrigins"];
    var allowedOrigins = ServiceCollectionExtensions.ParseCorsOrigins(corsOrigins);
    logger.Info($"Настроены разрешённые источники CORS: {string.Join(", ", allowedOrigins)}");
    builder.Services.AddCorsPolicy(allowedOrigins);

    builder.Services.AddOpenApiMetadata();
    builder.Services.Configure<ApiMetadata>(builder.Configuration.GetSection("ApiMetadata"));

    builder.Services.AddRateLimitingPolicies();

    builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
    builder.Services.AddMongoDb(logger);
    builder.Services.AddScoped<IMongoDatabase>(sp =>
    {
        var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
        var client = sp.GetRequiredService<IMongoClient>();
        return client.GetDatabase(settings.DatabaseName ?? "exerciser_db");
    });
    builder.Services.AddScoped<IExamRepository>(sp =>
    {
        var db = sp.GetRequiredService<IMongoDatabase>();
        var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
        return new ExamRepository(db, settings.ExamsCollectionName ?? "Exams");
    });
    builder.Services.AddScoped<IGroupRepository, GroupRepository>();
    builder.Services.AddScoped<ISessionRepository, SessionRepository>();
    builder.Services.AddScoped<IAttemptRepository, AttemptRepository>();

    var redisConnection = builder.Configuration["Redis:ConnectionString"];
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
    builder.Services.AddScoped<ICacheService, DistributedCacheService>();

    builder.Services.AddScoped<IExamImportValidator, ExamImportValidator>();
    builder.Services.AddScoped<IMongoDbMigrationService, MongoDbMigrationService>();

    #endregion

    var app = builder.Build();

    #region Инициализация базы данных

    using (var scope = app.Services.CreateScope())
    {
        var migration = scope.ServiceProvider.GetRequiredService<IMongoDbMigrationService>();
        await migration.InitializeAsync();
    }
    await CheckMongodbConnection(app.Services, logger);

    #endregion

    #region Настройка HTTP и OpenAPI

    var httpPort = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORT") ?? "8080";
    app.Urls.Clear();
    app.Urls.Add($"http://0.0.0.0:{httpPort}");

    app.MapOpenApi();
    var apiMetadata = app.Services.GetRequiredService<IOptions<ApiMetadata>>().Value;
    app.MapScalarApiReference(options =>
    {
        options.WithTitle(apiMetadata?.Title ?? "Exerciser API")
               .WithTheme(ScalarTheme.Alternate)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    #endregion

    #region Middleware и маршрутизация

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
        var nowLocal = DateTime.Now;
        var utcNow = DateTime.UtcNow;
        var timeZone = TimeZoneInfo.Local;
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
    logger.Fatal(ex, "Приложение завершило работу неожиданно");
    throw;
}
finally
{
    LogManager.Shutdown();
}

async Task CheckMongodbConnection(IServiceProvider services, NLog.ILogger logger)
{
    try
    {
        var client = services.GetRequiredService<IMongoClient>();
        var adminDb = client.GetDatabase("admin");
        var command = BsonDocument.Parse("{ ping: 1 }");
        await adminDb.RunCommandAsync<BsonDocument>(command);
        logger.Info("✓ Подключение к MongoDB успешно проверено");
    }
    catch (Exception ex)
    {
        logger.Error(ex, "✗ Не удалось подключиться к MongoDB. Проверьте строку подключения и учётные данные.");
        throw;
    }
}