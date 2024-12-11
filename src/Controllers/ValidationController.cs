namespace FabricDeploymentHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ValidationController : ControllerBase
{
    private readonly ILogger<ValidationController> _logger;
    private readonly IFabricRestService _fabricRestService;
    private readonly BlobServiceClient _blobServiceClient;

    public ValidationController(
        ILogger<ValidationController> logger,
        BlobServiceClient blobServiceClient,
        IFabricRestService fabricRestService
    )
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
        _fabricRestService = fabricRestService;
    }

    [HttpPost("validate-plan")]
    public async Task<IActionResult> ValidatePlan([FromBody] TenantDeploymentRequest request)
    {
        if (
            request == null
            || string.IsNullOrEmpty(request.PlanFile)
            || string.IsNullOrEmpty(request.RepoContainer)
        )
        {
            _logger.LogWarning("Received an invalid validation request. Missing required fields.");
            return BadRequest(
                new
                {
                    message = "Request body is missing or invalid. Please provide PlanFile and RepoContainer."
                }
            );
        }

        _logger.LogInformation(
            "Starting validation for plan file: {PlanFile} in container {RepoContainer}",
            request.PlanFile,
            request.RepoContainer
        );

        TenantDeploymentPlanResponse? tenantDeploymentPlan;
        try
        {
            tenantDeploymentPlan = await BlobUtils.LoadDeploymentPlanFromBlobAsync(
                _blobServiceClient,
                request.RepoContainer,
                request.PlanFile,
                _logger
            );

            if (tenantDeploymentPlan == null)
            {
                _logger.LogWarning(
                    "Failed to load deployment plan from {PlanFile} in container {RepoContainer}.",
                    request.PlanFile,
                    request.RepoContainer
                );
                return BadRequest(
                    new { message = "Deployment plan not found or could not be loaded." }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error loading deployment plan from {PlanFile} in container {RepoContainer}.",
                request.PlanFile,
                request.RepoContainer
            );
            return StatusCode(
                500,
                new { message = "An error occurred while loading the deployment plan." }
            );
        }

        foreach (var workspaceDeploymentPlan in tenantDeploymentPlan.Workspaces)
        {
            try
            {
                var items = await _fabricRestService.GetWorkspaceItemsAsync(
                    workspaceDeploymentPlan.WorkspaceId
                );

                foreach (var deploymentRequest in workspaceDeploymentPlan.DeploymentRequests)
                {
                    // in fabric its all by name
                    var existingItem = items.FirstOrDefault(
                        i => i.DisplayName == deploymentRequest.DisplayName
                    );

                    if (existingItem != null)
                    {
                        deploymentRequest.Validation = "Update";
                        deploymentRequest.Id = existingItem.Id; // Copy the Id from the existing item
                    }
                    else
                    {
                        deploymentRequest.Validation = "Create";
                        deploymentRequest.Id = Guid.Empty; // Ensure Id is reset for new items
                    }

                    deploymentRequest.TargetWorkspaceId = workspaceDeploymentPlan.WorkspaceId;
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(
                    httpEx,
                    "Error fetching items for workspace {WorkspaceId}.",
                    workspaceDeploymentPlan.WorkspaceId
                );
                foreach (var deploymentRequest in workspaceDeploymentPlan.DeploymentRequests)
                {
                    deploymentRequest.Validation = "Error";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled error during validation for workspace {WorkspaceId}.",
                    workspaceDeploymentPlan.WorkspaceId
                );
                foreach (var deploymentRequest in workspaceDeploymentPlan.DeploymentRequests)
                {
                    deploymentRequest.Validation = "Error";
                }
            }
        }

        _logger.LogInformation("Validation process completed for all workspaces.");
        var validatedPlanFileName = $"validated-{request.PlanFile}";
        try
        {
            // Save the validated plan to the same container

            await BlobUtils.SaveValidatedPlanToBlobAsync(
                _blobServiceClient,
                tenantDeploymentPlan,
                request.RepoContainer,
                validatedPlanFileName,
                _logger
            );

            _logger.LogInformation(
                "Validated deployment plan saved as {ValidatedPlanFileName} in container {RepoContainer}.",
                validatedPlanFileName,
                request.RepoContainer
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save validated deployment plan.");
            return StatusCode(
                500,
                new { message = "An error occurred while saving the validated deployment plan." }
            );
        }

        return Ok(
            new
            {
                message = "Validation completed and validated plan saved.",
                validatedPlanFileName,
                container = request.RepoContainer
            }
        );
    }
}
