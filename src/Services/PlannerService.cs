namespace FabricDeploymentHub.Services;
public class PlannerService : IPlannerService
{
    private readonly ILogger<PlannerService> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _configurationContainerName;
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;

    public WorkspaceConfigList WorkspaceConfigs { get; private set; } = new WorkspaceConfigList();
    public ItemTierConfig ItemTierConfigs { get; private set; } = new ItemTierConfig();

    public PlannerService(
        ILogger<PlannerService> logger,
        BlobServiceClient blobServiceClient,
        string configurationContainerName)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
        _configurationContainerName = configurationContainerName;

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        LoadConfigurations();
    }

    public void UpdateWorkspaceConfig(WorkspaceConfigList updatedConfig, bool saveToFile = false)
    {
        WorkspaceConfigs = updatedConfig;

        if (saveToFile)
        {
            try
            {
                var containerClient = GetContainerClient();
                var workspaceConfigBlob = containerClient.GetBlobClient("workspace-config.yml");
                var yaml = _serializer.Serialize(updatedConfig);
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(yaml));
                workspaceConfigBlob.Upload(stream, overwrite: true);

                _logger.LogInformation("Workspace configuration updated successfully in Blob Storage.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update workspace configuration in Blob Storage.");
                throw;
            }
        }
    }

    public void UpdateItemTierConfig(ItemTierConfig updatedConfig, bool saveToFile = false)
    {
        ItemTierConfigs = updatedConfig;

        if (saveToFile)
        {
            try
            {
                var containerClient = GetContainerClient();
                var itemTierConfigBlob = containerClient.GetBlobClient("item-tier-config.yml");
                var yaml = _serializer.Serialize(updatedConfig);
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(yaml));
                itemTierConfigBlob.Upload(stream, overwrite: true);

                _logger.LogInformation("Item tier configuration updated successfully in Blob Storage.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update item tier configuration in Blob Storage.");
                throw;
            }
        }
    }

    private BlobContainerClient GetContainerClient()
    {
        return _blobServiceClient.GetBlobContainerClient(_configurationContainerName);
    }

    private void LoadConfigurations()
    {
        try
        {
            var containerClient = GetContainerClient();

            // Load Workspace Configurations
            var workspaceConfigBlob = containerClient.GetBlobClient("workspace-config.yml");
            var workspaceConfigYaml = workspaceConfigBlob.DownloadContent().Value.Content.ToString();
            WorkspaceConfigs = _deserializer.Deserialize<WorkspaceConfigList>(workspaceConfigYaml);

            // Load Item Tier Configurations
            var itemTierConfigBlob = containerClient.GetBlobClient("item-tier-config.yml");
            var itemTierConfigYaml = itemTierConfigBlob.DownloadContent().Value.Content.ToString();
            ItemTierConfigs = _deserializer.Deserialize<ItemTierConfig>(itemTierConfigYaml);

            _logger.LogInformation("Configurations loaded successfully from Blob Storage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configurations.");
            throw;
        }
    }

    private bool IsEligibleForDeployment(PlatformMetadata platformMetadata, Guid workspaceId)
    {
        // Retrieve the workspace configuration
        var workspaceConfig = WorkspaceConfigs.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
        if (workspaceConfig == null)
        {
            _logger.LogWarning("Workspace ID {WorkspaceId} not found in configurations.", workspaceId);
            return false;
        }

        // Get the tier for the workspace
        var tier = workspaceConfig.Tier;
        if (string.IsNullOrEmpty(tier))
        {
            _logger.LogWarning("Workspace ID {WorkspaceId} does not have a valid tier.", workspaceId);
            return false;
        }

        // Check if the tier has the required item type
        if (!ItemTierConfigs.Tiers.TryGetValue(tier, out var tierConfig))
        {
            _logger.LogWarning("Tier {Tier} is not defined in the item tier configurations.", tier);
            return false;
        }

        // Check if the item type exists in the tier's Items dictionary
        if (!tierConfig.Items.TryGetValue(platformMetadata.Metadata.Type, out var allowedItems))
        {
            _logger.LogInformation("Item type {Type} is not allowed in tier {Tier}.", platformMetadata.Metadata.Type, tier);
            return false;
        }

        // Check if the item name or logicalId exists in the allowed items
        if (allowedItems.Contains(platformMetadata.Metadata.DisplayName))
        {
            _logger.LogInformation("Item {ItemName} of type {Type} is eligible for deployment to workspace {WorkspaceId}.", platformMetadata.Metadata.DisplayName, platformMetadata.Metadata.Type, workspaceId);
            return true;
        }

        _logger.LogInformation("Item {ItemName} of type {Type} is not eligible for deployment to workspace {WorkspaceId}.", platformMetadata.Metadata.DisplayName, platformMetadata.Metadata.Type, workspaceId);
        return false;
    }
}