namespace FabricDeploymentHub.Services;

public class PlannerService : IPlannerService
{
    private readonly ILogger<PlannerService> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IFabricTenantStateService _tenantStateService;

    private readonly IDependencyBindingService _dependencyBindingService;

    public PlannerService(
        ILogger<PlannerService> logger,
        BlobServiceClient blobServiceClient,
        IFabricTenantStateService tenantStateService,
        IDependencyBindingService dependencyBindingService
    )
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
        _tenantStateService = tenantStateService;
        _dependencyBindingService = dependencyBindingService;         
    }

    public async Task<TenantDeploymentPlanResponse> PlanTenantDeploymentAsync(
        TenantDeploymentPlanRequest tenantRequest
    )
    {
        var response = new TenantDeploymentPlanResponse();
        var itemTierConfig = await _tenantStateService.GetItemTierConfigsAsync();

        // Validate the tenant request
        var validationErrors = ValidationUtils.ValidateTenantDeploymentRequest(tenantRequest);
        if (validationErrors.Any())
        {
            response.Issues.AddRange(validationErrors);
            _logger.LogError(
                "Validation failed for TenantDeploymentRequest: {ValidationErrors}",
                validationErrors
            );
            return response;
        }

        var allWorkspaces = await _tenantStateService.GetAllWorkspacesAsync();

        // Process each workspace
        foreach (var workspaceId in allWorkspaces)
        {
            var workspaceResponse = await PlanWorkspaceDeploymentAsync(tenantRequest, workspaceId,itemTierConfig);
            if (workspaceResponse == null)
            {
                response.Issues.Add($"Failed to plan deployment for workspace {workspaceId}.");
                continue;
            }
            response.Messages.Add(
                $"Deployment planned for workspace {workspaceId} with {workspaceResponse.DeploymentRequests.Count} items."
            );
            response.Workspaces.Add(workspaceResponse);
        }

        response.SavedContainerName = $"{tenantRequest.RepoContainer}-deployment-plan";
        response.SavedPlanName = $"tenant-plan-{DateTime.UtcNow:yyyyMMddHHmmss}";
        // save the plan to blob storage if required
        if (tenantRequest.SavePlan)
        {
            response = await BlobUtils.SaveDeploymentPlanToBlobAsync(
                _blobServiceClient,
                response,
                response.SavedContainerName,
                response.SavedPlanName,
                _logger
            );
        }

        return response;
    }

    private bool ValidateWorkspaceConfig(
        Guid workspaceId,
        WorkspaceConfig? workspaceConfig,
        WorkspaceDeploymentPlanResponse workspaceResponse
    )
    {
        if (workspaceConfig == null)
        {
            _logger.LogWarning("Workspace ID {WorkspaceId} not found in configurations.", workspaceId);
            workspaceResponse.Issues.Add($"Workspace ID {workspaceId} not found in configurations.");
            return false;
        }

        return true;
    }

        private async Task ProcessItemForWorkspace(
        TenantDeploymentPlanRequest tenantRequest,
        Guid workspaceId,
        string folder,
        BlobContainerClient containerClient,
        WorkspaceConfig workspaceConfig,
        ItemTierConfig itemTierConfig,
        WorkspaceDeploymentPlanResponse workspaceResponse
    )
    {
        PlatformMetadata? platformMetadata;
        var folderContent = await BlobUtils.GetFolderContentsAsync(
            containerClient,
            folder,
            _logger
        );

        _logger.LogInformation(
            "Folder content keys for folder {Folder}: {Keys}",
            folder,
            string.Join(", ", folderContent.Keys.Select(key => $"'{key}'"))
        );

        if (!folderContent.TryGetValue(".platform", out var platformContent))
        {
            _logger.LogWarning("Platform metadata (.platform) not found in folder {Folder}.", folder);
            workspaceResponse.Issues.Add($"Platform metadata (.platform) missing in folder {folder}.");
            return;
        }

        // Deserialize the .platform content            
        try
        {
            platformMetadata = JsonSerializer.Deserialize<PlatformMetadata>(platformContent);
            if (platformMetadata == null)
            {
                throw new JsonException("Deserialization resulted in null object.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError( ex,"Failed to deserialize platform metadata from .platform file in folder {Folder}.", folder );
            workspaceResponse.Issues.Add($"Invalid .platform metadata in folder {folder}.");
            return; // Skip this folder if deserialization fails
        }

        if (!WorkspaceUtils.IsEligibleForDeployment(
                platformMetadata,
                workspaceId,
                workspaceConfig,
                itemTierConfig,
                _logger
            ))
        {
            workspaceResponse.Issues.Add(
                $"Item {platformMetadata.Metadata.DisplayName} is not eligible for deployment to workspace {workspaceId}."
            );
            return; // Skip folder if item is not eligible
        }

        var contentFileName = ItemTypeUtils.GetContentFileName(platformMetadata.Metadata.Type);
        if (!folderContent.TryGetValue(contentFileName, out var content))
        {
            _logger.LogWarning(
                "Content file {ContentFileName} not found in folder {Folder}.",
                contentFileName,
                folder
            );
            workspaceResponse.Issues.Add($"Content file {contentFileName} missing in folder {folder}.");
            return; // Skip folder if content is missing
        }
        try{
            // Process metadata and dependencies
            var metadata = ItemContentProcessor.ExtractMetadataFromContent(content, _logger);
            if (metadata == null)
            {
                workspaceResponse.Issues.Add($"Failed to extract metadata from content in folder {folder}.");
                return; // Skip folder if metadata extraction fails
            }

            metadata = await HandleLakehouseDependencies(metadata, workspaceId, workspaceResponse);
            metadata = await HandleEnvironmentDependencies(metadata, workspaceId, workspaceResponse);

            content = ItemContentProcessor.InjectMetadataIntoContent(content, metadata, _logger);
            folderContent[contentFileName] = content;

            var deploymentRequest = DeploymentRequestFactory.CreateDeploymentRequest(
                platformMetadata,
                workspaceId,
                folderContent,
                _logger
            );

            if (deploymentRequest == null)
            {
                workspaceResponse.Issues.Add(
                    $"Failed to create deployment request for item {platformMetadata.Metadata.DisplayName} in workspace {workspaceId}."
                );
                return; // Skip folder if deployment request creation fails
            }

            workspaceResponse.DeploymentRequests.Add(deploymentRequest);
            workspaceResponse.Messages.Add(
                $"Planned deployment for {platformMetadata.Metadata.DisplayName} in workspace {workspaceId}."
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Critical error processing folder {Folder} for workspace {WorkspaceId}.", folder, workspaceId);
            workspaceResponse.Issues.Add($"Critical error in folder {folder}: {ex.Message}");
            return; // Stop processing further for this folder
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing folder {Folder} for workspace {WorkspaceId}.", folder, workspaceId);
            workspaceResponse.Issues.Add($"Unexpected error in folder {folder}: {ex.Message}");
            return; // Stop processing further for this folder
        }

    }

    private async Task<WorkspaceDeploymentPlanResponse> PlanWorkspaceDeploymentAsync(
        TenantDeploymentPlanRequest tenantRequest,
        Guid workspaceId,
        ItemTierConfig itemTierConfig
    )
    {
        var workspaceResponse = new WorkspaceDeploymentPlanResponse { WorkspaceId = workspaceId };
        var workspaceConfig = await _tenantStateService.GetWorkspaceConfigAsync(workspaceId);        

        if (!ValidateWorkspaceConfig(workspaceId, workspaceConfig, workspaceResponse))
        {
            return workspaceResponse;
        }

        _logger.LogInformation(
            "Workspace {WorkspaceId} has {VariableCount} total variables.",
            workspaceId,
            workspaceConfig.Variables.Count
        );
        var containerClient = BlobUtils.GetContainerClient(_blobServiceClient, tenantRequest.RepoContainer);
        foreach (var folder in tenantRequest.ModifiedFolders)
        {
            try {
                await ProcessItemForWorkspace(
                    tenantRequest,
                    workspaceId,
                    folder,
                    containerClient,
                    workspaceConfig,
                    itemTierConfig,
                    workspaceResponse
                );
            } catch (Exception ex) {
                _logger.LogError(ex, "Error processing folder {Folder} for workspace {WorkspaceId}.", folder, workspaceId);
                workspaceResponse.Issues.Add($"Error processing folder {folder} for workspace {workspaceId}.");
            }
        }
        return workspaceResponse;
    }

    private async Task<MetaSection> HandleLakehouseDependencies(
        MetaSection metadata,
        Guid workspaceId,
        WorkspaceDeploymentPlanResponse workspaceResponse
    )
    {
        // check that there is a lakehouse dependency
        if (metadata.Dependencies == null || metadata.Dependencies.Lakehouse == null)
        {
            // since there are no lakehouse dependencies, return the metadata as is
            return metadata;
        }
        else
        {
            var lakehouseDependencyName = metadata.Dependencies.Lakehouse.DefaultLakehouseName;
            // Check if the lakehouse dependency name is null or empty
            if (string.IsNullOrWhiteSpace(lakehouseDependencyName))
            {
                throw new InvalidOperationException(
                    "Lakehouse dependency name is missing or empty. Unable to proceed with dependency resolution."
                );
            }

            var (found, lakehouseId, description) = await _dependencyBindingService.FindDependencyIdAsync(
                DependencyType.Lakehouse,
                lakehouseDependencyName,
                workspaceId
            );

            if (!found)
            {
                workspaceResponse.Issues.Add(
                    $"Issue finding Lakehouse dependency '{lakehouseDependencyName}' in workspace {workspaceId} with message {description}."
                );
                _logger.LogWarning(
                    "issue matching Lakehouse dependency '{LakehouseDependencyName}' in workspace {WorkspaceId} with message {MessageDescription}.",
                    lakehouseDependencyName,
                    workspaceId,
                    description
                );
                throw new InvalidOperationException(
                    $"Failed to find Lakehouse dependency '{lakehouseDependencyName}' in workspace {workspaceId}."
                );
            }
            else
            {
                metadata.Dependencies.Lakehouse.DefaultLakehouse = lakehouseId.ToString();
                metadata.Dependencies.Lakehouse.DefaultLakehouseWorkspaceId = workspaceId.ToString();
                metadata.Dependencies.Lakehouse.KnownLakehouses = new List<KnownLakehouse>
                {
                    new KnownLakehouse { Id = lakehouseId.ToString() }
                };

                _logger.LogInformation(
                    "Resolved lakehouse dependency '{LakehouseDependencyName}' to ID '{LakehouseId}' in workspace {WorkspaceId}.",
                    lakehouseDependencyName,
                    lakehouseId,
                    workspaceId
                );
            }
        }

        return metadata;
    }

    private async Task<MetaSection> HandleEnvironmentDependencies(
        MetaSection metadata,
        Guid workspaceId,
        WorkspaceDeploymentPlanResponse workspaceResponse
    )
    {
        // check if there is an environment dependency
        if (metadata.Dependencies!=null && metadata.Dependencies.Environment == null)
        {
            // since there are no environment dependencies, return the metadata as is
            return metadata;
        }
        // Both Dependency and Environment are not null if we got here
        if (!string.IsNullOrEmpty(metadata.Dependencies!.Environment!.EnvironmentId))
        {
            var environmentDependencyName = metadata.Dependencies.Environment.EnvironmentId;
            // Check if the environment dependency name is null or empty - there is no need to fire an api call to check
            if (string.IsNullOrWhiteSpace(environmentDependencyName))
            {
                throw new InvalidOperationException(
                    "Environment dependency name is missing or empty. Unable to proceed with dependency resolution."
                );
            }

            var (found, environmentId,description) = await _dependencyBindingService.FindDependencyIdAsync(
                DependencyType.Environment,
                environmentDependencyName,
                workspaceId
            );

            if (!found)
            {
                workspaceResponse.Issues.Add(
                    $"Issue finding Environment dependency '{environmentDependencyName}' in workspace {workspaceId} with error {description}."
                );
                _logger.LogError(
                    "Issue matching Environment dependency '{EnvironmentDependencyName}' for workspace {WorkspaceId} with error {MessageDescription}.",
                    environmentDependencyName,
                    workspaceId,
                    description
                );
                throw new InvalidOperationException(
                    $"Failed to find Environment dependency '{environmentDependencyName}' in workspace {workspaceId}."
                );
            }
            else
            {
                metadata.Dependencies.Environment.EnvironmentId = environmentId.ToString();
                metadata.Dependencies.Environment.WorkspaceId = workspaceId.ToString();

                _logger.LogInformation(
                    "Resolved environment dependency '{EnvironmentDependencyName}' to ID '{EnvironmentId}' in workspace {WorkspaceId}.",
                    environmentDependencyName,
                    environmentId,
                    workspaceId
                );
            }
        }

        return metadata;
    }
}
