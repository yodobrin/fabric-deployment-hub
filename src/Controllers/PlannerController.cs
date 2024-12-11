namespace FabricDeploymentHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlannerController : ControllerBase
{
    private readonly IPlannerService _plannerService;
    private readonly IFabricTenantStateService _tenantStateService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<PlannerController> _logger;

    public PlannerController(
        IPlannerService plannerService,
        IFabricTenantStateService tenantStateService,
        ITokenService tokenService,
        ILogger<PlannerController> logger
    )
    {
        _plannerService = plannerService;
        _tenantStateService = tenantStateService;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("tenant-deployment-plan")]
    public async Task<ActionResult> CreateTenantDeploymentPlan(
        [FromBody] TenantDeploymentPlanRequest request
    )
    {
        if (request == null)
        {
            _logger.LogWarning("Received a null or invalid request for tenant deployment plan.");
            return BadRequest(new { message = "Request body is missing or invalid." });
        }

        try
        {
            if (string.IsNullOrEmpty(request.RepoContainer))
            {
                _logger.LogWarning("Request validation failed: Missing RepoContainer.");
                return BadRequest(new { message = "RepoContainer is required." });
            }

            _logger.LogInformation("Fetching all workspaces for the tenant...");
            var workspaceIds = await _tenantStateService.GetAllWorkspacesAsync();

            if (!workspaceIds.Any())
            {
                _logger.LogWarning("No workspaces found for the tenant.");
                return BadRequest(new { message = "No workspaces available for planning." });
            }

            _logger.LogInformation(
                "Creating tenant deployment plan for {WorkspaceCount} workspaces.",
                workspaceIds.Count
            );

            var response = await _plannerService.PlanTenantDeploymentAsync(request);

            _logger.LogInformation("Tenant deployment plan created successfully.");

            // var serializedResponse = SerializePlanResponse(response, request.SavePlan);
            return Ok(
                new
                {
                    message = "Tenant deployment plan created successfully.",
                    planFileName = response.SavedPlanName,
                    container = response.SavedContainerName
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant deployment plan.");
            return StatusCode(
                500,
                new
                {
                    error = "An error occurred while creating the deployment plan.",
                    details = ex.Message
                }
            );
        }
    }

    /// <summary>
    /// Serializes the tenant deployment plan response. not used going forward was only used for test, consider removal.
    /// </summary>
    private object SerializePlanResponse(TenantDeploymentPlanResponse response, bool savePlan)
    {
        return new
        {
            workspaces = response.Workspaces.Select(
                workspace =>
                    new
                    {
                        workspaceId = workspace.WorkspaceId,
                        deploymentRequests = savePlan
                            ? null
                            : workspace.DeploymentRequests.Select(
                                request => request.GeneratePayload()
                            ),
                        issues = workspace.Issues,
                        hasErrors = workspace.HasErrors,
                        messages = workspace.Messages
                    }
            ),
            issues = response.Issues,
            hasErrors = response.HasErrors,
            messages = response.Messages
        };
    }
}
