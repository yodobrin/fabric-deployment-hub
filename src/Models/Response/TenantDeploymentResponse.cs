namespace FabricDeploymentHub.Models.Response;
public class TenantDeploymentResponse
{
    public List<WorkspaceDeploymentResponse> Workspaces { get; set; } = new List<WorkspaceDeploymentResponse>();
    public List<string> Issues { get; set; } = new List<string>(); // General issues with the tenant request
    public bool HasErrors => Issues.Any() || Workspaces.Any(w => w.HasErrors);
}

