

namespace FabricDeploymentHub.Controllers;

[ApiController]
[Route("api/workspace")]
public class WorkspaceController : ControllerBase
{
    private readonly IWorkspaceStateService _workspaceStateService;

    public WorkspaceController(IWorkspaceStateService workspaceStateService)
    {
        _workspaceStateService = workspaceStateService;
    }

    [HttpGet("{workspaceId}")]
    public async Task<IActionResult> GetWorkspaceState(Guid workspaceId)
    {
        try
        {
            var state = await _workspaceStateService.GetWorkspaceStateAsync(workspaceId);
            return Ok(state);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}