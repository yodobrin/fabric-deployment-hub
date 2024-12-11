namespace FabricDeploymentHub.Interfaces;

public interface IFabricTenantStateService
{
    /// <summary>
    /// Retrieves the list of all workspaces for the tenant.
    /// </summary>
    /// <returns>A list of workspace IDs.</returns>
    Task<List<Guid>> GetAllWorkspacesAsync();

    /// <summary>
    /// Retrieves metadata/configuration for a specific workspace.
    /// </summary>
    /// <param name="workspaceId">The ID of the workspace.</param>
    /// <returns>Metadata for the specified workspace.</returns>
    Task<WorkspaceConfig> GetWorkspaceConfigAsync(Guid workspaceId);

    /// <summary>
    /// Retrieves all workspace configurations.
    /// </summary>
    Task<WorkspaceConfigList> GetAllWorkspaceConfigsAsync();

    /// <summary>
    /// Retrieves item tier configurations for the tenant.
    /// </summary>
    Task<ItemTierConfig> GetItemTierConfigsAsync();
}
