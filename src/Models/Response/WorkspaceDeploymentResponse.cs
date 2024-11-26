namespace FabricDeploymentHub.Models.Response;


public class WorkspaceDeploymentResponse
{
    public Guid WorkspaceId { get; set; }
    public List<IDeploymentRequest> DeploymentRequests { get; set; } = new List<IDeploymentRequest>();
    public List<string> Issues { get; set; } = new List<string>(); // Workspace-specific issues
    public bool HasErrors => Issues.Any();
}