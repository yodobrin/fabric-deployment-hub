namespace FabricDeploymentHub.Services.Deployment;

public class DeploymentProcessor
{
    private readonly IFabricRestService _fabricRestService;
    private readonly ILogger<DeploymentProcessor> _logger;

    public DeploymentProcessor(
        IFabricRestService fabricRestService,
        ILogger<DeploymentProcessor> logger
    )
    {
        _fabricRestService = fabricRestService;
        _logger = logger;
    }

    public async Task<string> SendDeploymentRequestAsync(IDeploymentRequest deploymentRequest)
    {
        try
        {
            // Validate common fields
            if (
                string.IsNullOrWhiteSpace(deploymentRequest.DisplayName)
                || deploymentRequest.TargetWorkspaceId == Guid.Empty
            )
            {
                throw new ArgumentException("Deployment request is missing required fields.");
            }

            // Handle based on validation
            switch (deploymentRequest.Validation)
            {
                case "Create":
                    return await HandleCreateRequestAsync(deploymentRequest);

                case "Update":
                    return await HandleUpdateRequestAsync(deploymentRequest);

                default:
                    // Log and return a message for unsupported validation types
                    var errorMessage =
                        $"Deployment cannot proceed for item {deploymentRequest.DisplayName}. Validation status: {deploymentRequest.Validation}.";
                    _logger.LogWarning(errorMessage);
                    throw new InvalidOperationException(errorMessage);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid deployment request.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send deployment request.");
            throw;
        }
    }

    private async Task<string> HandleCreateRequestAsync(IDeploymentRequest deploymentRequest)
    {
        // Build URI dynamically based on item type
        var uri = BuildUri(deploymentRequest.Type, deploymentRequest.TargetWorkspaceId);
        var payload = deploymentRequest.GeneratePayload();
        var sanitizedPayload = deploymentRequest.SanitizePayload();

        _logger.LogInformation(
            "Creating new item. Payload: {Payload}",
            JsonSerializer.Serialize(
                sanitizedPayload,
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        return await _fabricRestService.PostAsync(uri, payload, waitForCompletion: true);
    }

    private async Task<string> HandleUpdateRequestAsync(IDeploymentRequest deploymentRequest)
    {
        if (deploymentRequest.Id == Guid.Empty)
        {
            var errorMessage =
                $"Cannot update item {deploymentRequest.DisplayName} in workspace {deploymentRequest.TargetWorkspaceId} because the Id is missing or invalid.";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        // Update-specific URI
        var uri = BuildUri(
            deploymentRequest.Type,
            deploymentRequest.TargetWorkspaceId,
            deploymentRequest.Id,
            isUpdate: true
        );
        var payload = deploymentRequest.GeneratePayload();
        var sanitizedPayload = deploymentRequest.SanitizePayload();

        _logger.LogInformation(
            "Updating item {Id}. Payload: {Payload}",
            deploymentRequest.Id,
            JsonSerializer.Serialize(
                sanitizedPayload,
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        return await _fabricRestService.PostAsync(uri, payload, waitForCompletion: true);
    }

    private string BuildUri(
        string itemType,
        Guid workspaceId,
        Guid? itemId = null,
        bool isUpdate = false
    )
    {
        // Map item type to endpoint
        var endpoint = itemType.ToLower() switch
        {
            "notebook" => "notebooks",
            "lakehouse" => "lakehouses",
            "datapipeline" => "dataPipelines",
            // Add more mappings as needed
            _ => throw new NotSupportedException($"Unsupported item type: {itemType}")
        };

        if (isUpdate && itemId.HasValue)
        {
            return $"workspaces/{workspaceId}/{endpoint}/{itemId}/updateDefinition?updateMetadata=True";
        }

        return $"workspaces/{workspaceId}/{endpoint}";
    }
}
