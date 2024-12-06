namespace FabricDeploymentHub.Models.State;
public class FabricItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("workspaceId")]
    public Guid WorkspaceId { get; set; }

    [JsonPropertyName("folderId")]
    public Guid FolderId { get; set; }
}