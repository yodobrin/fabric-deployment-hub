namespace FabricDeploymentHub.Models;

public class WorkspaceConfig
{
    public string Name { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // dev, test, prod
    public string Tier { get; set; } = string.Empty; // bronze, silver, gold
    public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>(); // Unified configuration
}

public class WorkspaceConfigList
{
    public List<WorkspaceConfig> Workspaces { get; set; } = new List<WorkspaceConfig>();
}
