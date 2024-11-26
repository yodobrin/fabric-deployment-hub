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
            
            // Construct the endpoint URI using the workspace ID - this is ONLY for notebook, need to revisit to dynamically create the uri
            var endpointUri = $"workspaces/{deploymentRequest.TargetWorkspaceId}/notebooks"; // Adjust for other item types if needed

            // Generate the payload
            var payload = deploymentRequest.GeneratePayload();

            // Log the request
            _logger.LogInformation("Sending deployment request to {EndpointUri}", endpointUri);

            // Call the Fabric REST API
            var response = await _fabricRestService.PostAsync(endpointUri, payload, waitForCompletion: true);

            // Log and return the response
            _logger.LogInformation("Deployment response: {Response}", response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send deployment request.");
            throw;
        }
    }
}