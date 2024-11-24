namespace FabricDeploymentHub.Interfaces;
public interface IWorkspaceStateService
{
    /// <summary>
    /// Retrieves the current state of the specified workspace, including all deployed items grouped by type.
    /// </summary>
    /// <param name="workspaceId">The unique ID of the workspace.</param>
    /// <returns>A Task representing the asynchronous operation, with a WorkspaceState as the result.</returns>
    Task<DeployedWorkspaceState> GetWorkspaceStateAsync(Guid workspaceId);
}