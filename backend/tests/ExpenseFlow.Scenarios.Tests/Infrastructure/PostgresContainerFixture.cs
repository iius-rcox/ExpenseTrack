using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace ExpenseFlow.Scenarios.Tests.Infrastructure;

/// <summary>
/// xUnit fixture that provides a PostgreSQL container for integration tests.
/// Uses Testcontainers to spin up an isolated database instance.
/// </summary>
public class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    /// <summary>
    /// Gets the connection string for the PostgreSQL container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Container not initialized");

    /// <summary>
    /// Gets the container host.
    /// </summary>
    public string Host => _container?.Hostname
        ?? throw new InvalidOperationException("Container not initialized");

    /// <summary>
    /// Gets the container port.
    /// </summary>
    public int Port => _container?.GetMappedPublicPort(5432)
        ?? throw new InvalidOperationException("Container not initialized");

    /// <summary>
    /// Initializes the PostgreSQL container.
    /// </summary>
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("expenseflow_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    /// <summary>
    /// Stops and removes the PostgreSQL container.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a new DbContextOptions configured for the container.
    /// </summary>
    public DbContextOptions<TContext> CreateDbContextOptions<TContext>()
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(ConnectionString)
            .Options;
    }

    /// <summary>
    /// Resets the database to a clean state by dropping all tables.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            DO $$ DECLARE
                r RECORD;
            BEGIN
                FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') LOOP
                    EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
                END LOOP;
            END $$;";

        await command.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// Collection definition for sharing PostgreSQL container across tests.
/// </summary>
[CollectionDefinition("PostgreSQL")]
public class PostgresCollectionDefinition : ICollectionFixture<PostgresContainerFixture>
{
}
