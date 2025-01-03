using System.ComponentModel.DataAnnotations;

namespace FabricDeploymentHub.Models.Request;

public class TenantDeploymentPlanRequest
{
    /// <summary>
    /// List of Workspace IDs where deployment is required.
    /// </summary>
    // [Required]
    // [MinLength(1, ErrorMessage = "At least one Workspace ID must be provided.")]
    // [JsonPropertyName("workspaceIds")]
    // public List<Guid> WorkspaceIds { get; set; } = new();

    /// <summary>
    /// The name of the repository container in Blob Storage.
    /// </summary>
    [Required]
    [JsonPropertyName("repoContainer")]
    public string RepoContainer { get; set; } = string.Empty;

    /// <summary>
    /// List of modified folders to process within the container.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one modified folder must be specified.")]
    [JsonPropertyName("modifiedFolders")]
    public List<string> ModifiedFolders { get; set; } = new();

    /// <summary>
    /// Flag to indicate if the plan should be saved.
    /// </summary>
    [JsonPropertyName("savePlan")]
    public bool SavePlan { get; set; } = false;
}
