using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Jobs;

/// <summary>
/// Test job for validating Hangfire dashboard and job processing.
/// </summary>
public class TestJob : JobBase
{
    public TestJob(ILogger<TestJob> logger) : base(logger)
    {
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var jobName = nameof(TestJob);
        var startTime = DateTime.UtcNow;

        LogJobStart(jobName);

        try
        {
            // Simulate some work
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            Logger.LogInformation("Test job executed successfully at {Time}", DateTime.UtcNow);

            LogJobComplete(jobName, DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            LogJobFailed(jobName, ex);
            throw;
        }
    }
}
