namespace FabricDeploymentHub.Services.Deployment;

public class DeploymentProcessor
{
    private readonly IFabricRestService _fabricRestService;
    private readonly ILogger<DeploymentProcessor> _logger;

    public DeploymentProcessor(IFabricRestService fabricRestService, ILogger<DeploymentProcessor> logger)
    {
        _fabricRestService = fabricRestService;
        _logger = logger;
    }

    public async Task<string> SendDeploymentRequestAsync(IDeploymentRequest deploymentRequest)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deploymentRequest.DisplayName) ||
                deploymentRequest.TargetWorkspaceId == Guid.Empty)
            {
                throw new ArgumentException("Deployment request is missing required fields.");
            }
            // TODO: change to be generic or add more switch cases for other item types
            var uri = $"workspaces/{deploymentRequest.TargetWorkspaceId}/notebooks";
            var payload = deploymentRequest.GeneratePayload();
            var sanitizedPayload = deploymentRequest.SanitizePayload();
            _logger.LogInformation("Validating payload for deployment request: {Payload}", JsonSerializer.Serialize(sanitizedPayload, new JsonSerializerOptions { WriteIndented = true }));

            return await _fabricRestService.PostAsync(uri, payload, waitForCompletion: true);
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
}