
namespace FabricDeploymentHub.Models.Response;

public class TenantDeploymentPlanResponse
{
    /// <summary>
    /// List of workspace-specific deployment plans.
    /// </summary>
    [JsonPropertyName("workspaces")]
    public List<WorkspaceDeploymentPlanResponse> Workspaces { get; set; } = new List<WorkspaceDeploymentPlanResponse>();

    /// <summary>
    /// General issues with the tenant request.
    /// </summary>
    [JsonPropertyName("issues")]
    public List<string> Issues { get; set; } = new List<string>();

    /// <summary>
    /// Indicates whether there are any errors in the plan.
    /// </summary>
    [JsonPropertyName("hasErrors")]
    public bool HasErrors => Issues.Any() || Workspaces.Any(w => w.HasErrors);

    /// <summary>
    /// Name of the container where the plan was saved, if applicable.
    /// </summary>
    [JsonPropertyName("savedContainerName")]
    public string? SavedContainerName { get; set; }

    /// <summary>
    /// Name of the saved plan, if applicable.
    /// </summary>
    [JsonPropertyName("savedPlanName")]
    public string? SavedPlanName { get; set; }

    /// <summary>
    /// Timestamp indicating when the plan was saved.
    /// </summary>
    [JsonPropertyName("savedTimestamp")]
    public DateTime? SavedTimestamp { get; set; }

    /// <summary>
    /// General messages about the operation.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<string> Messages { get; set; } = new List<string>();
}
