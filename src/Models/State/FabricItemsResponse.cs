namespace FabricDeploymentHub.Models.State;

public class FabricItemsResponse
{
    [JsonPropertyName("value")]
    public List<FabricItem> Value { get; set; } = new List<FabricItem>();
}
