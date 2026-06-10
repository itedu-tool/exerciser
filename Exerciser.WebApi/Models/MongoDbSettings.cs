namespace Exerciser.WebApi.Models;

public record MongoDbSettings
{
    public string? Host { get; init; }
    public int? Port { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? DatabaseName { get; init; }
    public string? ExamsCollectionName { get; init; }
}