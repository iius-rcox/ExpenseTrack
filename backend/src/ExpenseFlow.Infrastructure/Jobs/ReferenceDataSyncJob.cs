using ExpenseFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Jobs;

/// <summary>
/// Background job for syncing reference data from external SQL Server.
/// </summary>
public class ReferenceDataSyncJob : JobBase
{
    private readonly IReferenceDataService _referenceDataService;

    public ReferenceDataSyncJob(
        IReferenceDataService referenceDataService,
        ILogger<ReferenceDataSyncJob> logger) : base(logger)
    {
        _referenceDataService = referenceDataService;
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var jobName = nameof(ReferenceDataSyncJob);
        var startTime = DateTime.UtcNow;

        LogJobStart(jobName);

        try
        {
            var (glAccounts, departments, projects) = await _referenceDataService.SyncAllAsync();

            Logger.LogInformation(
                "Reference data sync complete: {GLAccounts} GL accounts, {Departments} departments, {Projects} projects",
                glAccounts, departments, projects);

            LogJobComplete(jobName, DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            LogJobFailed(jobName, ex);
            throw;
        }
    }
}
