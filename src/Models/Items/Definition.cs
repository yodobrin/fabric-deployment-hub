namespace FabricDeploymentHub.Models.Items;

public class Definition
{
    [JsonPropertyName("parts")]
    public List<Part> Parts { get; set; } = new List<Part>();
}
