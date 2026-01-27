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
    private string? _employeeId;
    private string? _supervisorName;
    private string? _departmentName;

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

    /// <summary>
    /// User's employee ID for expense reports. Set to null to clear.
    /// </summary>
    public string? EmployeeId
    {
        get => _employeeId;
        set
        {
            _employeeId = value;
            EmployeeIdProvided = true;
        }
    }

    /// <summary>
    /// User's supervisor name for expense reports. Set to null to clear.
    /// </summary>
    public string? SupervisorName
    {
        get => _supervisorName;
        set
        {
            _supervisorName = value;
            SupervisorNameProvided = true;
        }
    }

    /// <summary>
    /// User's department name for expense reports. Set to null to clear.
    /// </summary>
    public string? DepartmentName
    {
        get => _departmentName;
        set
        {
            _departmentName = value;
            DepartmentNameProvided = true;
        }
    }

    /// <summary>
    /// Indicates whether EmployeeId was explicitly provided in the request.
    /// </summary>
    [JsonIgnore]
    public bool EmployeeIdProvided { get; private set; }

    /// <summary>
    /// Indicates whether SupervisorName was explicitly provided in the request.
    /// </summary>
    [JsonIgnore]
    public bool SupervisorNameProvided { get; private set; }

    /// <summary>
    /// Indicates whether DepartmentName was explicitly provided in the request.
    /// </summary>
    [JsonIgnore]
    public bool DepartmentNameProvided { get; private set; }
}
