
namespace FabricDeploymentHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeploymentsController : ControllerBase
{
    private readonly ILogger<DeploymentsController> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly DeploymentProcessor _deploymentProcessor;

    public DeploymentsController(ILogger<DeploymentsController> logger, BlobServiceClient blobServiceClient, DeploymentProcessor deploymentProcessor)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
        _deploymentProcessor = deploymentProcessor;
    }
    [HttpPost]
    public async Task<IActionResult> DeployPlan([FromBody] TenantDeploymentRequest request)
    {
        // validate the request
        if (request == null || string.IsNullOrEmpty(request.PlanFile)  || string.IsNullOrEmpty(request.RepoContainer))
        {
            _logger.LogWarning("Received a null or invalid request for tenant deployment plan.");
            return BadRequest(new { message = "Request body is missing or invalid." });
        }
        _logger.LogInformation("Starting deployment for the plan file: {PlanFile} in container {RepoContainer}", request.PlanFile,request.RepoContainer);
        // read the plan into the TenantDeploymentPlanResponse
        var tenantDeploymentPlan = await BlobUtils.LoadDeploymentPlanFromBlobAsync(_blobServiceClient, request.RepoContainer, request.PlanFile, _logger);

        if (tenantDeploymentPlan == null)
        {
            _logger.LogWarning("Failed to load deployment plan from {PlanFile} in container {RepoContainer}.", request.PlanFile, request.RepoContainer);
            return BadRequest(new { message = "Failed to load deployment plan." });
        }
        _logger.LogInformation("Loaded deployment plan from {PlanFile} in container {RepoContainer}.", request.PlanFile, request.RepoContainer);
        // verify the plan does not have any errors - might be a useless check, as this is also checked before calling the controller
        if (tenantDeploymentPlan.HasErrors)
        {
            _logger.LogWarning("Deployment plan has errors. Cannot proceed with deployment.");
            return BadRequest(new { message = "Deployment plan has errors. Cannot proceed with deployment." });
        }
        var workspaceDeploymentPlans = tenantDeploymentPlan.Workspaces;
        // iterate through the workspace deployment plans
        foreach (var workspaceDeploymentPlan in workspaceDeploymentPlans)
        {
            _logger.LogInformation("Processing deployment plan for workspace {WorkspaceId}.", workspaceDeploymentPlan.WorkspaceId);
            // iterate through the deployment requests
            foreach (var deploymentRequest in workspaceDeploymentPlan.DeploymentRequests)
            {
                try
                {
                    var response = await _deploymentProcessor.SendDeploymentRequestAsync(deploymentRequest);
                    _logger.LogInformation("Deployment completed: {Response}", response);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing deployment request for: {DeploymentRequest}", JsonSerializer.Serialize(deploymentRequest));
                }
            }
        }
        _logger.LogInformation("All deployment requests have been processed.");
        return Ok(new { message = "Deployment requests processed successfully." });

    }
}