namespace FabricDeploymentHub.Models;

public class WorkspaceConfig
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // dev, test, prod
    public string Tier { get; set; } = string.Empty; // bronze, silver, gold
    public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Secrets { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
}

public class WorkspaceConfigList
{
    public List<WorkspaceConfig> Workspaces { get; set; } = new List<WorkspaceConfig>();
}
