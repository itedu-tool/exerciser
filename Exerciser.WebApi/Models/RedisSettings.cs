namespace Exerciser.WebApi.Models;

public record RedisSettings
{
    public string? ConnectionString { get; init; }
    public string? InstanceName { get; init; }
}