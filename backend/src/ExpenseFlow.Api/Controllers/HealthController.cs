using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for health check endpoints.
/// </summary>
public class HealthController : ApiControllerBase
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ExpenseFlowDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets the service health status.
    /// </summary>
    /// <returns>Health status with component checks.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponse>> GetHealth()
    {
        var checks = new Dictionary<string, string>();
        var isHealthy = true;

        // Database check
        try
        {
            await _dbContext.Database.CanConnectAsync();
            checks["database"] = "healthy";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
            checks["database"] = "unhealthy";
            isHealthy = false;
        }

        var response = new HealthResponse
        {
            Status = isHealthy ? "healthy" : "degraded",
            Timestamp = DateTime.UtcNow,
            Version = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Checks = checks
        };

        return isHealthy ? Ok(response) : StatusCode(503, response);
    }
}
