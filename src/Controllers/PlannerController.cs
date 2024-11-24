
namespace FabricDeploymentHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlannerController : ControllerBase
{
    private readonly PlannerService _plannerService;

    private readonly ITokenService _tokenService;

    public PlannerController(PlannerService plannerService, ITokenService tokenService)
    {
        _plannerService = plannerService;
        _tokenService = tokenService;
    }

    [HttpGet("workspace-configs")]
    public IActionResult GetWorkspaceConfigs()
    {
        return Ok(_plannerService.WorkspaceConfigs);
    }

    [HttpGet("item-tier-configs")]
    public IActionResult GetItemTierConfigs()
    {
        return Ok(_plannerService.ItemTierConfigs);
    }
}
