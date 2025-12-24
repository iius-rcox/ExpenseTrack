using Azure.Identity;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// SQL Server external data source with Managed Identity authentication.
/// </summary>
public class SqlServerDataSource : IExternalDataSource
{
    private readonly string _connectionString;
    private readonly ILogger<SqlServerDataSource> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public SqlServerDataSource(IConfiguration configuration, ILogger<SqlServerDataSource> logger)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("SqlServer connection string not configured");
        _logger = logger;

        // Configure Polly retry policy for transient failures
        _retryPolicy = Policy
            .Handle<SqlException>(ex => IsTransient(ex))
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "SQL Server connection attempt {RetryCount} failed. Retrying in {RetryDelay}s",
                        retryCount, timeSpan.TotalSeconds);
                });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GLAccount>> GetGLAccountsAsync()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var accounts = new List<GLAccount>();
            await using var connection = await CreateConnectionAsync();

            // Viewpoint bGLAC table: GLCo, GLAcct, Description, Active (Y/N)
            // GL codes like "40100.07" include dept suffix - extract left 5 chars for base GL code
            await using var command = new SqlCommand(
                @"SELECT DISTINCT
                    LEFT(CAST(GLAcct AS VARCHAR(50)), 5) AS Code,
                    MIN(ISNULL(Description, CAST(GLAcct AS VARCHAR(50)))) AS Name,
                    MIN(Description) AS Description,
                    MAX(CASE WHEN Active = 'Y' THEN 1 ELSE 0 END) AS IsActive
                FROM bGLAC
                WHERE Active = 'Y'
                GROUP BY LEFT(CAST(GLAcct AS VARCHAR(50)), 5)",
                connection);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                accounts.Add(new GLAccount
                {
                    Code = reader.GetString(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    IsActive = reader.GetInt32(3) == 1,
                    SyncedAt = DateTime.UtcNow
                });
            }

            _logger.LogDebug("Fetched {Count} GL accounts from SQL Server", accounts.Count);
            return accounts;
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var departments = new List<Department>();
            await using var connection = await CreateConnectionAsync();

            // Viewpoint bPRDP table: PRCo, PRDept, Description
            await using var command = new SqlCommand(
                @"SELECT DISTINCT
                    CAST(PRDept AS VARCHAR(50)) AS Code,
                    ISNULL(Description, CAST(PRDept AS VARCHAR(50))) AS Name,
                    Description,
                    1 AS IsActive
                FROM bPRDP",
                connection);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                departments.Add(new Department
                {
                    Code = reader.GetString(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    IsActive = reader.GetInt32(3) == 1,
                    SyncedAt = DateTime.UtcNow
                });
            }

            _logger.LogDebug("Fetched {Count} departments from SQL Server", departments.Count);
            return departments;
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Project>> GetProjectsAsync()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var projects = new List<Project>();
            await using var connection = await CreateConnectionAsync();

            // Viewpoint bJCCM table: JCCo, Contract, Description, ContractStatus (0=Open, 1=Soft Close, 2=Hard Close, 3=Closed)
            await using var command = new SqlCommand(
                @"SELECT
                    Contract AS Code,
                    ISNULL(Description, Contract) AS Name,
                    Description,
                    CASE WHEN ContractStatus = 0 THEN 1 ELSE 0 END AS IsActive
                FROM bJCCM
                WHERE ContractStatus = 0",
                connection);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                projects.Add(new Project
                {
                    Code = reader.GetString(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    IsActive = reader.GetInt32(3) == 1,
                    SyncedAt = DateTime.UtcNow
                });
            }

            _logger.LogDebug("Fetched {Count} projects from SQL Server", projects.Count);
            return projects;
        });
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var connection = await CreateConnectionAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQL Server connection test failed");
            return false;
        }
    }

    private async Task<SqlConnection> CreateConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);

        // Use Managed Identity for Azure AD authentication if configured
        if (_connectionString.Contains("Authentication=Active Directory Default", StringComparison.OrdinalIgnoreCase))
        {
            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(
                new Azure.Core.TokenRequestContext(new[] { "https://database.windows.net/.default" }));
            connection.AccessToken = token.Token;
        }

        await connection.OpenAsync();
        return connection;
    }

    private static bool IsTransient(SqlException ex)
    {
        // SQL Server transient error codes
        int[] transientErrors = { -2, 20, 64, 233, 10053, 10054, 10060, 40197, 40501, 40613 };
        return transientErrors.Contains(ex.Number);
    }
}
