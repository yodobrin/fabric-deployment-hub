namespace FabricDeploymentHub.Models.State;

public class DeployedWorkspaceState
{
    /// <summary>
    /// The unique ID of the workspace.
    /// </summary>
    public Guid WorkspaceId { get; set; } = Guid.Empty;

    /// <summary>
    /// List of deployed items grouped by type.
    /// </summary>
    public List<DeployedTypeState> ItemStates { get; set; } = new List<DeployedTypeState>();
}
