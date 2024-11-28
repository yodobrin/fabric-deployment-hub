using System.ComponentModel.DataAnnotations;

namespace FabricDeploymentHub.Models.Request;

public class TenantDeploymentRequest
{
    /// <summary>
    /// Indicates whether to use a pre-saved deployment plan.
    /// </summary>
    [JsonPropertyName("useSavedPlan")]
    public bool UseSavedPlan { get; set; } = false;

    /// <summary>
    /// The name of the repository container in Blob Storage.
    /// Required in both saved and unsaved plan scenarios.
    /// </summary>
    [Required]
    [JsonPropertyName("repoContainer")]
    public string RepoContainer { get; set; } = string.Empty;

    /// <summary>
    /// List of workspaces and their corresponding deployment requests.
    /// Required when not using a saved plan.
    /// </summary>
    [JsonPropertyName("workspaces")]
    public List<WorkspaceDeploymentRequest> Workspaces { get; set; } = new();
}