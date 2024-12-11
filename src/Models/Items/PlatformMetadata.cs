namespace FabricDeploymentHub.Models.Items;

public class PlatformMetadata
{
    [JsonPropertyName("metadata")]
    public Metadata Metadata { get; set; } = new Metadata();

    [JsonPropertyName("config")]
    public Config Config { get; set; } = new Config();
}

public class Metadata
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class Config
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("logicalId")]
    public Guid LogicalId { get; set; }
}
