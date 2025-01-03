namespace FabricDeploymentHub.Interfaces;

public interface IFabricRestService
{
    Task<string> GetAsync(string uri);
    Task<string> PostAsync(
        string uri,
        object payload,
        bool waitForCompletion = false,
        TimeSpan? pollingInterval = null,
        TimeSpan? timeout = null
    );
    Task<string> PatchAsync(
        string uri,
        object payload,
        bool waitForCompletion = false,
        TimeSpan? pollingInterval = null,
        TimeSpan? timeout = null
    );
    Task DeleteAsync(
        string uri,
        bool waitForCompletion = false,
        TimeSpan? pollingInterval = null,
        TimeSpan? timeout = null
    );

    Task<List<FabricItem>> GetWorkspaceItemsAsync(Guid workspaceId);
}
