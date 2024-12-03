
namespace FabricDeploymentHub.Services;

public class PlannerService : IPlannerService
{
    private readonly ILogger<PlannerService> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IFabricTenantStateService _tenantStateService;

    public PlannerService(
        ILogger<PlannerService> logger,
        BlobServiceClient blobServiceClient,
        IFabricTenantStateService tenantStateService)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
        _tenantStateService = tenantStateService;
    }

    public async Task<TenantDeploymentPlanResponse> PlanTenantDeploymentAsync(TenantDeploymentPlanRequest tenantRequest)
    {
        var response = new TenantDeploymentPlanResponse();

        // Validate the tenant request
        var validationErrors = ValidationUtils.ValidateTenantDeploymentRequest(tenantRequest);
        if (validationErrors.Any())
        {
            response.Issues.AddRange(validationErrors);
            _logger.LogError("Validation failed for TenantDeploymentRequest: {ValidationErrors}", validationErrors);
            return response;
        }

        var allWorkspaces = await _tenantStateService.GetAllWorkspacesAsync();

        // Process each workspace
        foreach (var workspaceId in allWorkspaces)
        {
            var workspaceResponse = await PlanWorkspaceDeploymentAsync(tenantRequest, workspaceId);
            if (workspaceResponse == null)
            {
                response.Issues.Add($"Failed to plan deployment for workspace {workspaceId}.");
                continue;
            }
            response.Messages.Add($"Deployment planned for workspace {workspaceId} with {workspaceResponse.DeploymentRequests.Count} items.");
            response.Workspaces.Add(workspaceResponse);
        }
        
        response.SavedContainerName = $"{tenantRequest.RepoContainer}-deployment-plan";
        response.SavedPlanName = $"tenant-plan-{DateTime.UtcNow:yyyyMMddHHmmss}";        
        // save the plan to blob storage if required
        if (tenantRequest.SavePlan)
        {
            response = await BlobUtils.SaveDeploymentPlanToBlobAsync(_blobServiceClient, response, _logger);

        }

        return response;
    }

    private async Task<WorkspaceDeploymentPlanResponse> PlanWorkspaceDeploymentAsync(TenantDeploymentPlanRequest tenantRequest, Guid workspaceId)
    {
        var workspaceResponse = new WorkspaceDeploymentPlanResponse { WorkspaceId = workspaceId };
        var workspaceConfig = await _tenantStateService.GetWorkspaceConfigAsync(workspaceId);
        var itemTierConfig = await _tenantStateService.GetItemTierConfigsAsync();

        if (workspaceConfig == null)
        {
            workspaceResponse.Issues.Add($"Workspace ID {workspaceId} not found in configurations.");
            return workspaceResponse;
        }

        foreach (var folder in tenantRequest.ModifiedFolders)
        {
            var platformMetadata = await BlobUtils.ParsePlatformMetadataAsync(
                    BlobUtils.GetContainerClient(_blobServiceClient, tenantRequest.RepoContainer),
                    folder,
                    _logger);
            // Check eligibility for deployment
            if (platformMetadata == null)
            {
                workspaceResponse.Issues.Add($"Invalid or missing metadata in folder: {folder}.");
                continue;
            }
            if (!WorkspaceUtils.IsEligibleForDeployment(platformMetadata, workspaceId, workspaceConfig ,itemTierConfig, _logger))
            {
                workspaceResponse.Issues.Add($"Item {platformMetadata.Metadata.DisplayName} is not eligible for deployment to workspace {workspaceId}.");
                continue;
            }
            workspaceResponse.Messages.Add($"Item {platformMetadata.Metadata.DisplayName} is eligible for deployment to workspace {workspaceId}.");
            // Create deployment request for eligible items
            var deploymentRequest = await DeploymentRequestFactory.CreateDeploymentRequestAsync(
                    platformMetadata,
                    folder,
                    workspaceId,
                    BlobUtils.GetContainerClient(_blobServiceClient, tenantRequest.RepoContainer), _logger);
            if(deploymentRequest == null)
            {
                workspaceResponse.Issues.Add($"Failed to create deployment request for item {platformMetadata.Metadata.DisplayName} in workspace {workspaceId}.");
                continue;
            }

            workspaceResponse.DeploymentRequests.Add(deploymentRequest);
            workspaceResponse.Messages.Add($"Planned deployment for {platformMetadata.Metadata.DisplayName} in workspace {workspaceId}.");
        }

        return workspaceResponse;
    }

    // private async Task LoadConfigurationsAsync()
    // {
    //     try
    //     {
    //         var containerClient = BlobUtils.GetContainerClient(_blobServiceClient, _configurationContainerName);

    //         // Load Workspace Configurations
    //         var workspaceConfigYaml = await BlobUtils.DownloadBlobContentAsync(containerClient, "workspace-config.yml");
    //         WorkspaceConfigs = YamlUtils.DeserializeYaml<WorkspaceConfigList>(workspaceConfigYaml);

    //         // Load Item Tier Configurations
    //         var itemTierConfigYaml = await BlobUtils.DownloadBlobContentAsync(containerClient, "item-tier-config.yml");
    //         ItemTierConfigs = YamlUtils.DeserializeYaml<ItemTierConfig>(itemTierConfigYaml);

    //         _logger.LogInformation("Configurations loaded successfully from Blob Storage.");
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Failed to load configurations.");
    //         throw;
    //     }
    // }
}