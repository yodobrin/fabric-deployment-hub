
namespace FabricDeploymentHub.Services;

public class PlannerService : IPlannerService
{
    private readonly ILogger<PlannerService> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _configurationContainerName;

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

        // Load configurations asynchronously
        LoadConfigurationsAsync().GetAwaiter().GetResult();
    }


    public async Task<TenantDeploymentResponse> PlanTenantDeploymentAsync(TenantDeploymentRequest tenantRequest)
    {
        var response = new TenantDeploymentResponse();

        // Validate the tenant request
        var validationErrors = ValidationUtils.ValidateTenantDeploymentRequest(tenantRequest);
        if (validationErrors.Any())
        {
            response.Issues.AddRange(validationErrors);
            _logger.LogError("Validation failed for TenantDeploymentRequest: {ValidationErrors}", validationErrors);
            return response;
        }

        // Process each workspace in the request
        foreach (var workspaceId in tenantRequest.WorkspaceIds)
        {
            var workspaceResponse = new WorkspaceDeploymentResponse { WorkspaceId = workspaceId };

            // Retrieve workspace configuration
            var workspaceConfig = WorkspaceUtils.GetWorkspaceConfig(WorkspaceConfigs, workspaceId, _logger);
            if (workspaceConfig == null)
            {
                workspaceResponse.Issues.Add($"Workspace ID {workspaceId} not found in configurations.");
                response.Workspaces.Add(workspaceResponse);
                continue;
            }

            // Process each modified folder
            foreach (var folder in tenantRequest.ModifiedFolders)
            {
                var platformMetadata = await BlobUtils.ParsePlatformMetadataAsync(
                    BlobUtils.GetContainerClient(_blobServiceClient, tenantRequest.RepoContainer),
                    folder,
                    _logger);

                if (platformMetadata == null)
                {
                    workspaceResponse.Issues.Add($"Invalid or missing metadata in folder: {folder}.");
                    continue;
                }

                // Check eligibility for deployment
                if (!WorkspaceUtils.IsEligibleForDeployment(platformMetadata, workspaceId, WorkspaceConfigs, ItemTierConfigs, _logger))
                {
                    workspaceResponse.Issues.Add($"Item {platformMetadata.Metadata.DisplayName} is not eligible for deployment to workspace {workspaceId}.");
                    continue;
                }

                // Create deployment request for eligible items
                var deploymentRequest = await DeploymentRequestFactory.CreateDeploymentRequestAsync(
                    platformMetadata,
                    folder,
                    workspaceId,
                    BlobUtils.GetContainerClient(_blobServiceClient, tenantRequest.RepoContainer), _logger);

                if (deploymentRequest != null)
                {
                    _logger.LogInformation($"Deployment request created for item {platformMetadata.Metadata.DisplayName} of type {platformMetadata.Metadata.Type} for workspace {workspaceId}. ");
                    workspaceResponse.DeploymentRequests.Add(deploymentRequest);   
                    workspaceResponse.Messages.Add($"Deployment planned for item {platformMetadata.Metadata.DisplayName} of type {platformMetadata.Metadata.Type} in workspace {workspaceId}.");                 
                }
            }

            response.Workspaces.Add(workspaceResponse);
        }
                // Save to blob storage if required
        if (tenantRequest.SavePlan)
        {
            response = await BlobUtils.SaveDeploymentPlanToBlobAsync(_blobServiceClient, response, tenantRequest.RepoContainer, _logger);
        }
        return response;
    }

    public async void UpdateWorkspaceConfig(WorkspaceConfigList updatedConfig, bool saveToFile = false)
    {
        WorkspaceConfigs = updatedConfig;

        if (saveToFile)
        {
            try
            {
                var containerClient = BlobUtils.GetContainerClient(_blobServiceClient, _configurationContainerName);
                var yamlContent = YamlUtils.SerializeToYaml(updatedConfig);
                await BlobUtils.UploadBlobContentAsync(containerClient, "workspace-config.yml", yamlContent);
                _logger.LogInformation("Workspace configuration updated successfully in Blob Storage.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update workspace configuration in Blob Storage.");
                throw;
            }
        }
    }

    public async void UpdateItemTierConfig(ItemTierConfig updatedConfig, bool saveToFile = false)
    {
        ItemTierConfigs = updatedConfig;

        if (saveToFile)
        {
            try
            {
                var containerClient = BlobUtils.GetContainerClient(_blobServiceClient, _configurationContainerName);
                var yamlContent = YamlUtils.SerializeToYaml(updatedConfig);
                await BlobUtils.UploadBlobContentAsync(containerClient, "item-tier-config.yml", yamlContent);
                _logger.LogInformation("Item tier configuration updated successfully in Blob Storage.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update item tier configuration in Blob Storage.");
                throw;
            }
        }
    }

    private async Task LoadConfigurationsAsync()
    {
        try
        {
            var containerClient = BlobUtils.GetContainerClient(_blobServiceClient, _configurationContainerName);

            // Load Workspace Configurations
            var workspaceConfigYaml = await BlobUtils.DownloadBlobContentAsync(containerClient, "workspace-config.yml");
            WorkspaceConfigs = YamlUtils.DeserializeYaml<WorkspaceConfigList>(workspaceConfigYaml);

            // Load Item Tier Configurations
            var itemTierConfigYaml = await BlobUtils.DownloadBlobContentAsync(containerClient, "item-tier-config.yml");
            ItemTierConfigs = YamlUtils.DeserializeYaml<ItemTierConfig>(itemTierConfigYaml);

            _logger.LogInformation("Configurations loaded successfully from Blob Storage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configurations.");
            throw;
        }
    }
}