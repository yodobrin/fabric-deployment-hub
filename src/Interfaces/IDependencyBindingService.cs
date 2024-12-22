namespace FabricDeploymentHub.Interfaces;

public interface IDependencyBindingService
{
    /// <summary>
    /// Searches for a dependency by name within the given workspace.
    /// </summary>
    /// <param name="dependencyType">The type of dependency (e.g., Lakehouse, Environment).</param>
    /// <param name="dependencyName">The name of the dependency to search for.</param>
    /// <param name="workspaceId">The ID of the target workspace.</param>
    /// <returns>A tuple indicating success and the corresponding GUID of the dependency if found, or Guid.Empty if not.</returns>
    Task<(bool Found, Guid DependencyId, string Description)> FindDependencyIdAsync(
        DependencyType dependencyType,
        string dependencyName,
        Guid workspaceId
    );
}
