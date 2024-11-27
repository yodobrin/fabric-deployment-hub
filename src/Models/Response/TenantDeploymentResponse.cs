namespace FabricDeploymentHub.Models.Response;
public class TenantDeploymentResponse
{
    public List<WorkspaceDeploymentResponse> Workspaces { get; set; } = new List<WorkspaceDeploymentResponse>();
    public List<string> Issues { get; set; } = new List<string>(); // General issues with the tenant request
    public bool HasErrors => Issues.Any() || Workspaces.Any(w => w.HasErrors);
    public string? SavedContainerName { get; set; } // Populated if SavePlan is true    
    public DateTime? SavedTimestamp { get; set; } // When the plan was saved
    public List<string> Messages { get; set; } = new List<string>(); // General messages about the operation
}

