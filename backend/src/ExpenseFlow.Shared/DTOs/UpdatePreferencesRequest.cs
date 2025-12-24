using System.Text.Json.Serialization;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request DTO for updating user preferences.
/// All fields are optional for partial updates.
/// Uses backing fields to track whether optional fields were explicitly provided.
/// </summary>
public class UpdatePreferencesRequest
{
    private Guid? _defaultDepartmentId;
    private Guid? _defaultProjectId;

    /// <summary>
    /// Theme preference. Valid values: "light", "dark", "system".
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// Default department ID for new expense reports. Set to null to clear.
    /// </summary>
    public Guid? DefaultDepartmentId
    {
        get => _defaultDepartmentId;
        set
        {
            _defaultDepartmentId = value;
            DefaultDepartmentIdProvided = true;
        }
    }

    /// <summary>
    /// Default project ID for new expense reports. Set to null to clear.
    /// </summary>
    public Guid? DefaultProjectId
    {
        get => _defaultProjectId;
        set
        {
            _defaultProjectId = value;
            DefaultProjectIdProvided = true;
        }
    }

    /// <summary>
    /// Indicates whether DefaultDepartmentId was explicitly provided in the request.
    /// </summary>
    [JsonIgnore]
    public bool DefaultDepartmentIdProvided { get; private set; }

    /// <summary>
    /// Indicates whether DefaultProjectId was explicitly provided in the request.
    /// </summary>
    [JsonIgnore]
    public bool DefaultProjectIdProvided { get; private set; }
}
