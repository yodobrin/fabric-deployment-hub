namespace FabricDeploymentHub.Services;

public class FabricTenantStateService : IFabricTenantStateService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _configurationContainerName;
    private readonly ILogger<FabricTenantStateService> _logger;
    private WorkspaceConfigList? _workspaceConfigs;
    private ItemTierConfig? _itemTierConfigs;

    public FabricTenantStateService(
        BlobServiceClient blobServiceClient,
        string configurationContainerName,
        ILogger<FabricTenantStateService> logger
    )
    {
        _blobServiceClient = blobServiceClient;
        _configurationContainerName = configurationContainerName;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var containerClient = BlobUtils.GetContainerClient(
                _blobServiceClient,
                _configurationContainerName
            );

            // Load Workspace Configurations
            var workspaceConfigYaml = await BlobUtils.DownloadBlobContentAsync(
                containerClient,
                "workspace-config.yml"
            );
            _workspaceConfigs = YamlUtils.DeserializeYaml<WorkspaceConfigList>(workspaceConfigYaml);

            // Load Item Tier Configurations
            var itemTierConfigYaml = await BlobUtils.DownloadBlobContentAsync(
                containerClient,
                "item-tier-config.yml"
            );
            _itemTierConfigs = YamlUtils.DeserializeYaml<ItemTierConfig>(itemTierConfigYaml);

            _logger.LogInformation("Configurations successfully loaded from Blob Storage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize and load configurations.");
            throw;
        }
    }

    private static readonly List<string> SupportedItemTypes =
        new() { "notebook", "datapipeline", "dataset", "report" };

    public async Task<List<Guid>> GetAllWorkspacesAsync()
    {
        if (_workspaceConfigs == null)
            await InitializeAsync();

        return _workspaceConfigs!.Workspaces.Select(w => w.Id).ToList();
    }

    public async Task<WorkspaceConfig> GetWorkspaceConfigAsync(Guid workspaceId)
    {
        if (_workspaceConfigs == null)
            await InitializeAsync();
        // var configs = await GetAllWorkspaceConfigsAsync();
        return _workspaceConfigs!.Workspaces.FirstOrDefault(w => w.Id == workspaceId)
            ?? throw new KeyNotFoundException($"Workspace with ID {workspaceId} not found.");
    }

    // public async Task<DeployedWorkspaceState> GetWorkspaceStateAsync(Guid workspaceId)
    // {
    //     // Logic to fetch the deployed state for the given workspace
    //     // This might involve querying other services or blob storage
    //     throw new NotImplementedException("Implement workspace state retrieval logic here.");
    // }

    public async Task<WorkspaceConfigList> GetAllWorkspaceConfigsAsync()
    {
        if (_workspaceConfigs == null)
            await InitializeAsync();

        return _workspaceConfigs!;
    }

    public async Task<ItemTierConfig> GetItemTierConfigsAsync()
    {
        if (_itemTierConfigs == null)
            await InitializeAsync();

        return _itemTierConfigs!;
    }
}
