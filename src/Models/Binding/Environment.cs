namespace FabricDeploymentHub.Models.Binding;

public class EnvironmentResponse
{
    [JsonPropertyName("value")]
    public List<EnvironmentDetails> Value { get; set; } = new();
}

public class EnvironmentDetails
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public Properties Properties { get; set; } = new();
}

public class Properties
{
    [JsonPropertyName("publishDetails")]
    public PublishDetails PublishDetails { get; set; } = new();
}

public class PublishDetails
{
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("targetVersion")]
    public string TargetVersion { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = string.Empty;

    [JsonPropertyName("componentPublishInfo")]
    public ComponentPublishInfo ComponentPublishInfo { get; set; } = new();
}

public class ComponentPublishInfo
{
    [JsonPropertyName("sparkLibraries")]
    public ComponentState SparkLibraries { get; set; } = new();

    [JsonPropertyName("sparkSettings")]
    public ComponentState SparkSettings { get; set; } = new();
}

public class ComponentState
{
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
}
