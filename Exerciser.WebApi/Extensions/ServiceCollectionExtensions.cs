using System;
using System.Linq;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using MongoDB.Driver;
using Exerciser.WebApi.Models;

namespace Exerciser.WebApi.Extensions;

/// <summary>Методы расширения для регистрации сервисов в DI контейнер.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Добавить CORS политику с переданными origins.</summary>
        public IServiceCollection AddCorsPolicy(string[] allowedOrigins)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowConfiguredOrigins", policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithExposedHeaders("Content-Disposition", "X-Total-Count");
                });
            });
            return services;
        }

        /// <summary>Добавить OpenAPI с кастомными метаданными.</summary>
        public IServiceCollection AddOpenApiMetadata()
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, _) =>
                {
                    ApiMetadata metadata = context.ApplicationServices
                        .GetRequiredService<IOptions<ApiMetadata>>().Value;

                    document.Info = new OpenApiInfo
                    {
                        Title = metadata.Title ?? "Exerciser API",
                        Version = metadata.Version ?? "1.0.0",
                        Description = metadata.Description,
                        TermsOfService = string.IsNullOrEmpty(metadata.TermsOfService)
                            ? null
                            : new Uri(metadata.TermsOfService),
                        Contact = new OpenApiContact
                        {
                            Name = metadata.Contact?.Name,
                            Email = metadata.Contact?.Email,
                            Url = string.IsNullOrEmpty(metadata.Contact?.Url)
                                ? null
                                : new Uri(metadata.Contact.Url)
                        },
                        License = new OpenApiLicense
                        {
                            Name = metadata.License?.Name,
                            Url = string.IsNullOrEmpty(metadata.License?.Url)
                                ? null
                                : new Uri(metadata.License.Url)
                        }
                    };
                    return System.Threading.Tasks.Task.CompletedTask;
                });
            });
            return services;
        }

        /// <summary>Добавить MongoDB клиент с пулированием и конфигурацией.</summary>
        public IServiceCollection AddMongoDb(ILogger logger)
        {
            services.AddSingleton<IMongoClient>(sp =>
            {
                MongoDbSettings settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                string host = settings.Host ?? "mongodb";
                int port = settings.Port ?? 27017;
                string database = settings.DatabaseName ?? "exerciser_db";

                string connectionString = string.IsNullOrEmpty(settings.Username)
                    ? $"mongodb://{host}:{port}/{database}"
                    : $"mongodb://{settings.Username}:{settings.Password}@{host}:{port}/{database}?authSource=admin";

                MongoClientSettings? mongoSettings = MongoClientSettings.FromConnectionString(connectionString);

                mongoSettings.MaxConnectionPoolSize = 50;
                mongoSettings.MinConnectionPoolSize = 10;
                mongoSettings.MaxConnectionIdleTime = TimeSpan.FromMinutes(10);
                mongoSettings.ConnectTimeout = TimeSpan.FromSeconds(10);
                mongoSettings.SocketTimeout = TimeSpan.FromSeconds(10);
                mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

                logger.LogInformation("MongoDB pooling configured (max: 50, min: 10)");
                return new MongoClient(mongoSettings);
            });

            return services;
        }

        /// <summary>Добавить Rate Limiting policies.</summary>
        public IServiceCollection AddRateLimitingPolicies()
        {
            services.AddRateLimiter(limiterOptions =>
            {
                limiterOptions.AddFixedWindowLimiter("fixed", options =>
                {
                    options.PermitLimit = 100;
                    options.Window = TimeSpan.FromMinutes(1);
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = 10;
                });

                limiterOptions.AddSlidingWindowLimiter("import-sliding", options =>
                {
                    options.PermitLimit = 10;
                    options.Window = TimeSpan.FromHours(1);
                    options.SegmentsPerWindow = 4;
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = 5;
                });

                limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            return services;
        }
    }

    /// <summary>Получить массив CORS origins из строки.</summary>
    public static string[] ParseCorsOrigins(string? originsString)
    {
        if (string.IsNullOrWhiteSpace(originsString))
        {
            return ["http://localhost:3000", "http://localhost:5000"];
        }

        return originsString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim())
            .Where(o => !string.IsNullOrEmpty(o))
            .ToArray();
    }
}