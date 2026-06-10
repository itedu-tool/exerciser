# Восстановление и сборка
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Копирование и восстановление зависимостей (слой кеша)
COPY ["Exerciser.WebApi/Exerciser.WebApi.csproj", "Exerciser.WebApi/"]
COPY ["Exerciser.slnx", "./"]
RUN dotnet restore "Exerciser.WebApi/Exerciser.WebApi.csproj"

# Копирование исходного кода (слой кеша)
COPY . .

# Сборка и публикация
WORKDIR /src/Exerciser.WebApi
RUN dotnet publish "./Exerciser.WebApi.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# Финальный образ runtime-only
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

WORKDIR /app

# Установка curl для healthcheck
USER root
RUN apk add --no-cache curl
RUN addgroup -g 1001 -S dotnetapp && \
    adduser -u 1001 -S dotnetapp -G dotnetapp && \
    chown -R dotnetapp:dotnetapp /app

USER dotnetapp

EXPOSE 8080 8081

# Копирование опубликованного приложения
COPY --from=build --chown=dotnetapp:dotnetapp /app/publish .

# Корректный HEALTHCHECK: проверяет эндпоинт /health
HEALTHCHECK --interval=30s --timeout=10s --retries=3 --start-period=10s \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Exerciser.WebApi.dll"]