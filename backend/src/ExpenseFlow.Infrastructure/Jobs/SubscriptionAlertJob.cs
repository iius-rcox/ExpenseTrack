using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Jobs;

/// <summary>
/// Background job for checking subscription status and generating alerts.
/// Runs monthly to detect missing subscriptions and generate alerts for users.
/// </summary>
public class SubscriptionAlertJob : JobBase
{
    private readonly ISubscriptionDetectionService _subscriptionService;
    private readonly ExpenseFlowDbContext _dbContext;

    public SubscriptionAlertJob(
        ISubscriptionDetectionService subscriptionService,
        ExpenseFlowDbContext dbContext,
        ILogger<SubscriptionAlertJob> logger) : base(logger)
    {
        _subscriptionService = subscriptionService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Executes the monthly subscription check for all users with active subscriptions.
    /// </summary>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var jobName = nameof(SubscriptionAlertJob);
        var startTime = DateTime.UtcNow;

        LogJobStart(jobName);

        try
        {
            // Get previous month in YYYY-MM format for checking
            var previousMonth = DateTime.UtcNow.AddMonths(-1);
            var monthToCheck = previousMonth.ToString("yyyy-MM");

            // Get all users with active subscriptions
            var userIds = await _dbContext.DetectedSubscriptions
                .Where(s => s.Status == Shared.Enums.SubscriptionStatus.Active)
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            Logger.LogInformation(
                "Running subscription check for {Month} across {UserCount} users",
                monthToCheck, userIds.Count);

            var totalAlerts = 0;

            foreach (var userId in userIds)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var alerts = await _subscriptionService.RunMonthlyCheckAsync(userId, monthToCheck);
                    totalAlerts += alerts.Count;

                    if (alerts.Count > 0)
                    {
                        Logger.LogInformation(
                            "Generated {AlertCount} subscription alerts for user {UserId}",
                            alerts.Count, userId);
                    }
                }
                catch (Exception ex)
                {
                    // Log but continue processing other users
                    Logger.LogWarning(ex,
                        "Failed to process subscription check for user {UserId}",
                        userId);
                }
            }

            Logger.LogInformation(
                "Subscription alert check complete: {TotalAlerts} alerts generated for {UserCount} users",
                totalAlerts, userIds.Count);

            LogJobComplete(jobName, DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            LogJobFailed(jobName, ex);
            throw;
        }
    }

    /// <summary>
    /// Runs a subscription check for a specific user.
    /// Can be triggered manually from an endpoint.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="month">The month to check (YYYY-MM format).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExecuteForUserAsync(Guid userId, string month, CancellationToken cancellationToken)
    {
        var jobName = $"{nameof(SubscriptionAlertJob)}_User_{userId}";
        var startTime = DateTime.UtcNow;

        LogJobStart(jobName);

        try
        {
            var alerts = await _subscriptionService.RunMonthlyCheckAsync(userId, month);

            Logger.LogInformation(
                "Generated {AlertCount} subscription alerts for user {UserId} for month {Month}",
                alerts.Count, userId, month);

            LogJobComplete(jobName, DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            LogJobFailed(jobName, ex);
            throw;
        }
    }
}
