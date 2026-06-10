#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Docker Compose Service Manager для Exerciser.WebApi.

.DESCRIPTION
    Управление сервисами (mongodb, webapi) через Docker Compose.
    Поддерживает запуск, остановку, перезапуск, очистку, просмотр логов,
    health check, подключение к MongoDB и интерактивное меню.

.PARAMETER Action
    Действие: Start, Stop, Restart, Clean, FullRestart, Status, Logs, Test, Mongo, Build, CleanImages, Menu.

.PARAMETER Service
    Сервис: all, mongodb, webapi (для действий Logs, Stop, Restart). По умолчанию all.

.PARAMETER Tail
    Количество строк логов (для Logs). По умолчанию 50.

.PARAMETER Follow
    Следить за логами в реальном времени (для Logs). По умолчанию $false.

.PARAMETER Verbose
    Выводить подробную информацию.

.EXAMPLE
    .\manage-services.ps1 -Action Start
    .\manage-services.ps1 -Action FullRestart
    .\manage-services.ps1 -Action Logs -Service webapi -Tail 100 -Follow
    .\manage-services.ps1 -Action Test
    .\manage-services.ps1 -Action Mongo
    .\manage-services.ps1 -Action Menu
#>

param(
    [ValidateSet('Start', 'Stop', 'Restart', 'Clean', 'FullRestart', 'Status', 'Logs', 'Test', 'Mongo', 'Build', 'CleanImages', 'Menu')]
    [string]$Action = 'Status',

    [ValidateSet('mongodb', 'webapi', 'all')]
    [string]$Service = 'all',

    [int]$Tail = 50,

    [switch]$Follow,

    [switch]$Verbose
)

# ==================== ЦВЕТА И ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ====================

$Colors = @{
    Reset = "`e[0m"
    Bold = "`e[1m"
    Dim = "`e[2m"
    Black = "`e[30m"
    Red = "`e[31m"
    Green = "`e[32m"
    Yellow = "`e[33m"
    Blue = "`e[34m"
    Magenta = "`e[35m"
    Cyan = "`e[36m"
    White = "`e[37m"
}

function Write-ColorOutput
{
    param([string]$Message, [string]$Color = 'White', [switch]$NoNewline)
    $code = $Colors[$Color] ?? $Colors['White']
    $output = "$code$Message$( $Colors['Reset'] )"
    if ($NoNewline)
    {
        Write-Host $output -NoNewline
    }
    else
    {
        Write-Host $output
    }
}

function Write-Section
{
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput "╔═════════════════════════════════════════════════════════════╗" -Color Cyan
    Write-ColorOutput "║ $($Title.PadRight(61) ) ║" -Color Cyan
    Write-ColorOutput "╚═════════════════════════════════════════════════════════════╝" -Color Cyan
    Write-Host ""
}

function Write-Status
{
    param([string]$Message, [ValidateSet('Info', 'Success', 'Warning', 'Error', 'Debug')][string]$Type = 'Info')
    $timestamp = Get-Date -Format "HH:mm:ss"
    $icon = @{ Info = "ℹ️"; Success = "✅"; Warning = "⚠️"; Error = "❌"; Debug = "🐛" }
    $color = @{ Info = 'Cyan'; Success = 'Green'; Warning = 'Yellow'; Error = 'Red'; Debug = 'Magenta' }
    Write-ColorOutput "[$timestamp] " -Color Dim -NoNewline
    Write-ColorOutput "$( $icon[$Type] ) $Message" -Color $color[$Type]
}

function Test-DockerInstalled
{
    if (-not (Get-Command docker -ErrorAction SilentlyContinue))
    {
        Write-Status "Docker is not installed or not in PATH" -Type Error
        exit 1
    }
    # Проверка команды docker compose
    $composeVersion = docker compose version 2>&1
    if ($LASTEXITCODE -ne 0)
    {
        Write-Status "Docker Compose plugin is not available (try 'docker-compose' instead?)" -Type Error
        Write-Status "Please install Docker Desktop or Docker Engine with Compose plugin." -Type Error
        exit 1
    }
    return $true
}

function Get-ServiceStatus
{
    param([string]$ServiceName)
    try
    {
        $output = docker compose ps $ServiceName 2>&1
        if ($output -match 'healthy')
        {
            return 'healthy'
        }
        elseif ($output -match 'running')
        {
            return 'running'
        }
        elseif ($output -match 'exited')
        {
            return 'exited'
        }
        else
        {
            return 'unknown'
        }
    }
    catch
    {
        return 'error'
    }
}

function Show-ServiceStatus
{
    Write-Section "SERVICE STATUS"
    $services = @('mongodb', 'webapi')
    foreach ($svc in $services)
    {
        $status = Get-ServiceStatus -ServiceName $svc
        $color = @{ healthy = 'Green'; running = 'Yellow'; exited = 'Red'; unknown = 'Red'; error = 'Red' }[$status]
        Write-ColorOutput "  • $($svc.ToUpper() ) ... " -Color Cyan -NoNewline
        Write-ColorOutput $status -Color $color
    }
    Write-Host ""
}

# ==================== ОСНОВНЫЕ ДЕЙСТВИЯ ====================

function Stop-Services
{
    Write-Section "STOPPING SERVICES"
    Write-Status "Stopping docker compose services..." -Type Info
    try
    {
        $output = docker compose down 2>&1
        if ($LASTEXITCODE -eq 0)
        {
            Write-Status "Services stopped successfully" -Type Success
            if ($Verbose)
            {
                Write-Host $output
            }
        }
        else
        {
            Write-Status "Failed to stop services" -Type Error
            Write-Host $output
            return $false
        }
    }
    catch
    {
        Write-Status "Error: $_" -Type Error
        return $false
    }
    return $true
}

function Clean-Volumes
{
    Write-Section "CLEANING VOLUMES"
    Write-Status "Removing persistent volumes..." -Type Warning
    try
    {
        $output = docker compose down -v 2>&1
        if ($LASTEXITCODE -eq 0)
        {
            Write-Status "All volumes removed" -Type Success
            if ($Verbose)
            {
                Write-Host $output
            }
        }
        else
        {
            Write-Status "Failed to remove volumes" -Type Error
            Write-Host $output
            return $false
        }
    }
    catch
    {
        Write-Status "Error: $_" -Type Error
        return $false
    }
    return $true
}

function Clean-Images
{
    Write-Section "CLEANING IMAGES"
    Write-Status "Removing unused Docker images..." -Type Info
    try
    {
        $output = docker image prune -f 2>&1
        if ($LASTEXITCODE -eq 0)
        {
            Write-Status "Unused images cleaned" -Type Success
            if ($Verbose)
            {
                Write-Host $output
            }
        }
    }
    catch
    {
        Write-Status "Warning: Could not clean images" -Type Warning
    }
}

function Start-Services
{
    Write-Section "STARTING SERVICES"
    Write-Status "Building and starting docker compose services..." -Type Info
    try
    {
        $output = docker compose up -d 2>&1
        if ($LASTEXITCODE -eq 0)
        {
            Write-Status "Services started" -Type Success
        }
        else
        {
            Write-Status "Build completed with warnings" -Type Warning
            if ($Verbose)
            {
                Write-Host $output
            }
        }
    }
    catch
    {
        Write-Status "Error starting services: $_" -Type Error
        return $false
    }
    return $true
}

function Wait-ForServicesHealthy
{
    Write-Section "HEALTH CHECK"
    $maxWait = 60
    $elapsed = 0
    $healthy = $false
    Write-Status "Waiting for services to become healthy (max ${maxWait}s)..." -Type Info
    Write-Host ""
    while ($elapsed -lt $maxWait)
    {
        $mongo = Get-ServiceStatus -ServiceName 'mongodb'
        $web = Get-ServiceStatus -ServiceName 'webapi'
        Write-ColorOutput "  [$elapsed/${maxWait}s] MongoDB: " -Color Dim -NoNewline
        Write-ColorOutput $mongo -Color (@{ healthy = 'Green'; running = 'Yellow' }[$mongo] ?? 'Red') -NoNewline
        Write-ColorOutput " | WebAPI: " -Color Dim -NoNewline
        Write-ColorOutput $web -Color (@{ healthy = 'Green'; running = 'Yellow' }[$web] ?? 'Red')
        if ($mongo -eq 'healthy' -and $web -eq 'healthy')
        {
            Write-Host ""
            Write-Status "All services are healthy! ✨" -Type Success
            $healthy = $true
            break
        }
        Start-Sleep 5
        $elapsed += 5
    }
    if (-not $healthy)
    {
        Write-Host ""
        Write-Status "Services did not become healthy within ${maxWait}s" -Type Warning
        Write-Status "Check logs with: docker compose logs" -Type Info
    }
    return $healthy
}

function Show-Logs
{
    param([string]$ServiceName, [int]$Lines, [switch]$FollowMode)
    $svc = if ($ServiceName -eq 'all')
    {
        ''
    }
    else
    {
        $ServiceName
    }
    $cmd = "docker compose logs $svc --tail=$Lines"
    if ($FollowMode)
    {
        $cmd += " --follow"
    }
    Write-Section "LOGS ($ServiceName)"
    Invoke-Expression $cmd
}

function Show-Test
{
    Write-Section "API HEALTH CHECK"
    $url = "http://localhost:8080/health"
    try
    {
        $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -ErrorAction Stop
        if ($resp.StatusCode -eq 200)
        {
            Write-Status "API is healthy! ✅" -Type Success
            $json = $resp.Content | ConvertFrom-Json
            Write-Host ($json | Format-List | Out-String)
        }
        else
        {
            Write-Status "API returned status: $( $resp.StatusCode )" -Type Error
        }
    }
    catch
    {
        Write-Status "Could not connect to API at $url : $_" -Type Error
    }
}

function Show-Mongo
{
    Write-Section "CONNECTING TO MONGODB"
    Write-Status "Opening MongoDB shell..." -Type Info
    docker compose exec mongodb mongosh --eval "db.adminCommand('ping')"
}

function Build-Image
{
    Write-Section "BUILDING DOCKER IMAGE"
    Write-Status "Building exerciser-webapi:latest..." -Type Info
    $result = docker build -f Exerciser.WebApi/Dockerfile -t exerciser-webapi:latest . 2>&1
    if ($LASTEXITCODE -eq 0)
    {
        Write-Status "Image built successfully" -Type Success
    }
    else
    {
        Write-Status "Image build failed" -Type Error
        Write-Host $result
    }
}

# ==================== ИНТЕРАКТИВНОЕ МЕНЮ ====================

function Show-InteractiveMenu
{
    Clear-Host
    $services = @('mongodb', 'webapi')

    while ($true)
    {
        # Получение статусов
        $statuses = @{ }
        foreach ($s in $services)
        {
            $statuses[$s] = Get-ServiceStatus -ServiceName $s
        }

        # Заголовок
        Write-ColorOutput "╔══════════════════════════════════════════════════════════════════╗" -Color Magenta
        Write-ColorOutput "║                      Exerciser.WebApi – Управление               ║" -Color Magenta
        Write-ColorOutput "╚══════════════════════════════════════════════════════════════════╝" -Color Magenta
        Write-Host ""

        # Статус сервисов
        Write-ColorOutput "Текущее состояние сервисов:" -Color Cyan
        foreach ($s in $services)
        {
            $status = $statuses[$s]
            $color = switch ($status)
            {
                'healthy' {
                    'Green'
                }
                'running' {
                    'Yellow'
                }
                default   {
                    'Red'
                }
            }
            Write-ColorOutput "  • $($s.ToUpper() ) : " -Color White -NoNewline
            Write-ColorOutput $status -Color $color
        }
        Write-Host ""

        # Меню действий
        Write-ColorOutput "Доступные действия:" -Color Cyan
        $menuItems = @(
            @{ Key = "1"; Desc = "▶  Запустить все сервисы (Start)" }
            @{ Key = "2"; Desc = "⏹  Остановить все сервисы (Stop)" }
            @{ Key = "3"; Desc = "🔄  Перезапустить сервисы (Restart)" }
            @{ Key = "4"; Desc = "🧹  Полная очистка + перезапуск (FullRestart)" }
            @{ Key = "5"; Desc = "📊  Показать статус" }
            @{ Key = "6"; Desc = "📜  Показать логи (все, $Tail строк)" }
            @{ Key = "7"; Desc = "🗄   Логи MongoDB (следование)" }
            @{ Key = "8"; Desc = "🌐  Логи WebAPI (следование)" }
            @{ Key = "9"; Desc = "🏥  Проверить Health API" }
            @{ Key = "10"; Desc = "🐚  Подключиться к MongoDB shell" }
            @{ Key = "11"; Desc = "🔨  Пересобрать Docker образ" }
            @{ Key = "12"; Desc = "🗑   Очистить неиспользуемые образы" }
            @{ Key = "R"; Desc = "🔄  Обновить статус" }
            @{ Key = "Q"; Desc = "🚪  Выйти" }
        )

        foreach ($item in $menuItems)
        {
            Write-ColorOutput "  [$( $item.Key )]" -Color Cyan -NoNewline
            Write-ColorOutput " $( $item.Desc )" -Color White
        }

        Write-Host ""
        $choice = Read-Host "Ваш выбор"
        Write-Host ""

        switch ( $choice.ToUpper())
        {
            '1' {
                Start-Services; Wait-ForServicesHealthy
            }
            '2' {
                Stop-Services
            }
            '3' {
                Stop-Services; Start-Services; Wait-ForServicesHealthy
            }
            '4' {
                Stop-Services; Clean-Volumes; Clean-Images; Start-Services; Wait-ForServicesHealthy
            }
            '5' {
                Show-ServiceStatus
            }
            '6' {
                Show-Logs -ServiceName all -Lines $Tail -FollowMode $false; pause "Нажмите Enter для продолжения..."
            }
            '7' {
                Show-Logs -ServiceName mongodb -Lines $Tail -FollowMode $true
            }
            '8' {
                Show-Logs -ServiceName webapi -Lines $Tail -FollowMode $true
            }
            '9' {
                Show-Test; pause "Нажмите Enter для продолжения..."
            }
            '10' {
                Show-Mongo; pause "Нажмите Enter для продолжения..."
            }
            '11' {
                Build-Image; pause "Нажмите Enter для продолжения..."
            }
            '12' {
                Clean-Images; pause "Нажмите Enter для продолжения..."
            }
            'R' {
                Clear-Host; continue
            }
            'Q' {
                Write-ColorOutput "До свидания!" -Color Cyan; exit 0
            }
            default {
                Write-Status "Неверный выбор, попробуйте снова" -Type Warning; Start-Sleep -Seconds 1
            }
        }

        # После некоторых действий не нужно очищать экран (логи, shell, выход)
        if ($choice.ToUpper() -notin @('3', '4', '6', '7', '8', '9', '10', '11', '12', 'R', 'Q'))
        {
            Clear-Host
        }
        elseif ($choice.ToUpper() -in @('6', '9', '10', '11', '12'))
        {
            # Для этих действий очищаем после паузы
            Clear-Host
        }
    }
}

# ==================== MAIN ====================

# Проверка Docker
Test-DockerInstalled

# Баннер при запуске без интерактивного меню
if ($Action -ne 'Menu')
{
    Write-Host ""
    Write-ColorOutput "╔════════════════════════════════════════════════════════════╗" -Color Magenta
    Write-ColorOutput "║          Docker Compose Service Manager                   ║" -Color Magenta
    Write-ColorOutput "║                     Exerciser.WebApi                      ║" -Color Magenta
    Write-ColorOutput "╚════════════════════════════════════════════════════════════╝" -Color Magenta
    Write-Host ""
}

# Выполнение действия
switch ($Action)
{
    'Status'      {
        Show-ServiceStatus
    }
    'Start'       {
        Start-Services; Wait-ForServicesHealthy
    }
    'Stop'        {
        Stop-Services
    }
    'Restart'     {
        Stop-Services; Start-Services; Wait-ForServicesHealthy
    }
    'Clean'       {
        Stop-Services; Clean-Volumes
    }
    'FullRestart' {
        Stop-Services; Clean-Volumes; Clean-Images; Start-Services; Wait-ForServicesHealthy
    }
    'CleanImages' {
        Clean-Images
    }
    'Build'       {
        Build-Image
    }
    'Logs'        {
        Show-Logs -ServiceName $Service -Lines $Tail -FollowMode:$Follow
    }
    'Test'        {
        Show-Test
    }
    'Mongo'       {
        Show-Mongo
    }
    'Menu'        {
        Show-InteractiveMenu
    }
    default       {
        Write-Status "Unknown action: $Action" -Type Error
    }
}

if ($Action -ne 'Menu')
{
    Write-ColorOutput "════════════════════════════════════════════════════════════" -Color Magenta
    Write-Host ""
}