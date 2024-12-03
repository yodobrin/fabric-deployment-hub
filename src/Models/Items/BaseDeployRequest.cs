namespace FabricDeploymentHub.Models.Items;

public abstract class BaseDeployRequest : IDeploymentRequest
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("targetWorkspaceId")]
    public Guid TargetWorkspaceId { get; set; }
    public string Validation { get; set; } = "pending";

    public virtual object GeneratePayload()
    {
        // Base implementation for shared fields
        return new
        {
            displayName = DisplayName,
            description = Description,
            targetWorkspaceId = TargetWorkspaceId,
            validation = Validation
        };
    }
}