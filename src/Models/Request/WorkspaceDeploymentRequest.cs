using System.ComponentModel.DataAnnotations;

namespace FabricDeploymentHub.Models.Request;

/// <summary>
/// Represents a workspace and its associated deployment requests.
/// </summary>
public class WorkspaceDeploymentRequest
{
    /// <summary>
    /// The ID of the workspace.
    /// </summary>
    [Required]
    [JsonPropertyName("workspaceId")]
    public Guid WorkspaceId { get; set; }

    /// <summary>
    /// List of deployment requests for the workspace.
    /// </summary>
    [JsonPropertyName("deploymentRequests")]
    public List<IDeploymentRequest> DeploymentRequests { get; set; } = new();
}