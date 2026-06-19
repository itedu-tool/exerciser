using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Exerciser.WebApi.Tests.Fixtures;

public class MongoDbFixture : IAsyncLifetime
{
    public MongoDbContainer Container { get; } = new MongoDbBuilder()
        .WithImage("mongo:8.3")
        .Build();

    public IMongoClient Client { get; private set; } = null!;
    public IMongoDatabase Database { get; private set; } = null!;
    public const string DatabaseName = "test_db";

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        Client = new MongoClient(Container.GetConnectionString());
        Database = Client.GetDatabase(DatabaseName);
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }

    public async Task ClearCollectionsAsync()
    {
        var collections = await Database.ListCollectionNamesAsync();
        foreach (var name in await collections.ToListAsync())
        {
            await Database.DropCollectionAsync(name);
        }
    }
}