namespace FabricDeploymentHub.Models.Binding;

public class MetaSection
{
    [JsonPropertyName("kernel_info")]
    public KernelInfo KernelInfo { get; set; } = new();

    [JsonPropertyName("dependencies")]
    public DependencyBlock? Dependencies { get; set; } 
}

public class KernelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class DependencyBlock
{
    [JsonPropertyName("lakehouse")]
    public LakehouseDependency? Lakehouse { get; set; }

    [JsonPropertyName("environment")]
    public EnvironmentDependency? Environment { get; set; }
}

public class LakehouseDependency
{
    [JsonPropertyName("default_lakehouse")]
    public string DefaultLakehouse { get; set; } = string.Empty;

    [JsonPropertyName("default_lakehouse_name")]
    public string DefaultLakehouseName { get; set; } = string.Empty;

    [JsonPropertyName("default_lakehouse_workspace_id")]
    public string DefaultLakehouseWorkspaceId { get; set; } = string.Empty;

    [JsonPropertyName("known_lakehouses")]
    public List<KnownLakehouse> KnownLakehouses { get; set; } = new();
}

public class KnownLakehouse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class EnvironmentDependency
{
    [JsonPropertyName("environmentId")]
    public string EnvironmentId { get; set; } = string.Empty;

    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;
}
