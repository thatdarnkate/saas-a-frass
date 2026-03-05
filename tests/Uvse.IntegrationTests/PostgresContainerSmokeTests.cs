using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Uvse.IntegrationTests;

public sealed class PostgresContainerSmokeTests : IAsyncLifetime
{
    private readonly PostgreSqlTestcontainer _container = new TestcontainersBuilder<PostgreSqlTestcontainer>()
        .WithDatabase(new PostgreSqlTestcontainerConfiguration
        {
            Database = "uvse",
            Username = "uvse",
            Password = "uvse"
        })
        .Build();

    [Fact(Skip = "Requires Docker runtime in CI or local dev.")]
    public async Task Postgres_Container_Should_Start()
    {
        await _container.StartAsync();
        Assert.False(string.IsNullOrWhiteSpace(_container.ConnectionString));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
