using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for user profile and preferences management.
/// </summary>
[Authorize]
[Route("api/user")] // Support singular /api/user/ routes for frontend compatibility
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserPreferencesService _preferencesService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        IUserPreferencesService preferencesService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _preferencesService = preferencesService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user's profile including preferences.
    /// Auto-creates profile on first login.
    /// </summary>
    /// <returns>The current user's profile with preferences.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var prefs = await _preferencesService.GetOrCreateDefaultsAsync(user.Id);

        _logger.LogDebug(
            "Retrieved profile for user {UserId} with theme {Theme}",
            user.Id,
            prefs.Theme);

        var response = new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            // Fall back to email if DisplayName is null or empty (per FR-002)
            DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email : user.DisplayName,
            Department = user.Department,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Preferences = new UserPreferencesResponse
            {
                Theme = prefs.Theme,
                DefaultDepartmentId = prefs.DefaultDepartmentId,
                DefaultProjectId = prefs.DefaultProjectId,
                EmployeeId = prefs.EmployeeId,
                SupervisorName = prefs.SupervisorName,
                DepartmentName = prefs.DepartmentName
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets the current user's preferences.
    /// Returns system defaults if no preferences have been explicitly set.
    /// </summary>
    /// <returns>The current user's preferences.</returns>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(UserPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserPreferencesResponse>> GetPreferences()
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var prefs = await _preferencesService.GetOrCreateDefaultsAsync(user.Id);

        _logger.LogDebug(
            "Retrieved preferences for user {UserId}: Theme={Theme}",
            user.Id,
            prefs.Theme);

        return Ok(new UserPreferencesResponse
        {
            Theme = prefs.Theme,
            DefaultDepartmentId = prefs.DefaultDepartmentId,
            DefaultProjectId = prefs.DefaultProjectId,
            EmployeeId = prefs.EmployeeId,
            SupervisorName = prefs.SupervisorName,
            DepartmentName = prefs.DepartmentName
        });
    }

    /// <summary>
    /// Partially updates the current user's preferences.
    /// Only provided fields are updated; omitted fields remain unchanged.
    /// Creates a preferences record if one doesn't exist (upsert).
    /// </summary>
    /// <param name="request">The partial update request.</param>
    /// <returns>The updated preferences.</returns>
    [HttpPatch("preferences")]
    [ProducesResponseType(typeof(UserPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserPreferencesResponse>> UpdatePreferences(
        [FromBody] UpdatePreferencesRequest request)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var prefs = await _preferencesService.UpdateAsync(user.Id, request);

            _logger.LogInformation(
                "Updated preferences for user {UserId}: Theme={Theme}",
                user.Id,
                prefs.Theme);

            return Ok(new UserPreferencesResponse
            {
                Theme = prefs.Theme,
                DefaultDepartmentId = prefs.DefaultDepartmentId,
                DefaultProjectId = prefs.DefaultProjectId,
                EmployeeId = prefs.EmployeeId,
                SupervisorName = prefs.SupervisorName,
                DepartmentName = prefs.DepartmentName
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                "Invalid preference update for user {UserId}: {Message}",
                user.Id,
                ex.Message);

            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
    }
}
