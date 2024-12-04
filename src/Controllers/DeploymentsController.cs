namespace FabricDeploymentHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeploymentsController : ControllerBase
{
    private readonly ILogger<DeploymentsController> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly DeploymentProcessor _deploymentProcessor;

    public DeploymentsController(
        ILogger<DeploymentsController> logger,
        BlobServiceClient blobServiceClient,
        DeploymentProcessor deploymentProcessor)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
        _deploymentProcessor = deploymentProcessor;
    }

    [HttpPost("deploy-plan")]
    public async Task<IActionResult> DeployPlan([FromBody] TenantDeploymentRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.PlanFile) || string.IsNullOrEmpty(request.RepoContainer))
        {
            _logger.LogWarning("Invalid deployment request received. Missing required fields.");
            return BadRequest(new { message = "Request body is missing or invalid. Ensure PlanFile and RepoContainer are provided." });
        }

        _logger.LogInformation("Starting deployment for plan file: {PlanFile} in container {RepoContainer}", request.PlanFile, request.RepoContainer);

        TenantDeploymentPlanResponse? tenantDeploymentPlan;

        try
        {
            tenantDeploymentPlan = await BlobUtils.LoadDeploymentPlanFromBlobAsync(_blobServiceClient, request.RepoContainer, request.PlanFile, _logger);

            if (tenantDeploymentPlan == null)
            {
                _logger.LogWarning("Deployment plan could not be loaded from {PlanFile} in container {RepoContainer}.", request.PlanFile, request.RepoContainer);
                return BadRequest(new { message = "Deployment plan not found or could not be loaded." });
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON parsing error for deployment plan from {PlanFile} in container {RepoContainer}.", request.PlanFile, request.RepoContainer);
            return BadRequest(new { message = "Deployment plan contains invalid JSON. Please check the file structure.", error = jsonEx.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while loading deployment plan from {PlanFile} in container {RepoContainer}.", request.PlanFile, request.RepoContainer);
            return StatusCode(500, new { message = "An unexpected error occurred while loading the deployment plan.", error = ex.Message });
        }

        if (tenantDeploymentPlan.HasErrors)
        {
            _logger.LogWarning("Deployment plan contains errors: {Issues}", string.Join(", ", tenantDeploymentPlan.Issues));
            return BadRequest(new { message = "Deployment plan contains errors and cannot proceed.", issues = tenantDeploymentPlan.Issues });
        }

        var deploymentErrors = new List<object>();
        var workspaceDeploymentPlans = tenantDeploymentPlan.Workspaces;

        foreach (var workspaceDeploymentPlan in workspaceDeploymentPlans)
        {
            _logger.LogInformation("Processing deployment plan for workspace {WorkspaceId}.", workspaceDeploymentPlan.WorkspaceId);

            foreach (var deploymentRequest in workspaceDeploymentPlan.DeploymentRequests)
            {
                try
                {
                    deploymentRequest.TargetWorkspaceId = workspaceDeploymentPlan.WorkspaceId;

                    // Validate the deployment request payload before sending
                    if (!ValidateDeploymentRequest(deploymentRequest))
                    {
                        _logger.LogWarning("Invalid deployment request for {Item} in workspace {WorkspaceId}.", deploymentRequest.DisplayName, workspaceDeploymentPlan.WorkspaceId);
                        deploymentErrors.Add(new
                        {
                            workspaceId = workspaceDeploymentPlan.WorkspaceId,
                            item = deploymentRequest.DisplayName,
                            message = "Deployment request is invalid or missing required fields."
                        });
                        continue;
                    }

                    var response = await _deploymentProcessor.SendDeploymentRequestAsync(deploymentRequest);
                    _logger.LogInformation("Deployment successful for {Item} in workspace {WorkspaceId}. Response: {Response}", deploymentRequest.DisplayName, workspaceDeploymentPlan.WorkspaceId, response);
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError(httpEx, "HTTP error during deployment for {Item} in workspace {WorkspaceId}.", deploymentRequest.DisplayName, workspaceDeploymentPlan.WorkspaceId);
                    deploymentErrors.Add(new
                    {
                        workspaceId = workspaceDeploymentPlan.WorkspaceId,
                        item = deploymentRequest.DisplayName,
                        error = "HTTP error during deployment.",
                        details = httpEx.Message
                    });
                }
                catch (TimeoutException timeoutEx)
                {
                    _logger.LogError(timeoutEx, "Timeout error during deployment for {Item} in workspace {WorkspaceId}.", deploymentRequest.DisplayName, workspaceDeploymentPlan.WorkspaceId);
                    deploymentErrors.Add(new
                    {
                        workspaceId = workspaceDeploymentPlan.WorkspaceId,
                        item = deploymentRequest.DisplayName,
                        error = "Deployment timed out.",
                        details = timeoutEx.Message
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error during deployment for {Item} in workspace {WorkspaceId}.", deploymentRequest.DisplayName, workspaceDeploymentPlan.WorkspaceId);
                    deploymentErrors.Add(new
                    {
                        workspaceId = workspaceDeploymentPlan.WorkspaceId,
                        item = deploymentRequest.DisplayName,
                        error = "Unexpected error during deployment.",
                        details = ex.Message
                    });
                }
            }
        }

        if (deploymentErrors.Any())
        {
            _logger.LogWarning("Deployment completed with errors for some requests.");
            return StatusCode(207, new
            {
                message = "Deployment completed with errors. Check the errors for more details.",
                errors = deploymentErrors
            });
        }

        _logger.LogInformation("Deployment process completed successfully for all requests.");
        return Ok(new { message = "All deployment requests processed successfully." });
    }

    private bool ValidateDeploymentRequest(IDeploymentRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.DisplayName) && request.TargetWorkspaceId != Guid.Empty;
    }
}