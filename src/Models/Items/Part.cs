namespace FabricDeploymentHub.Models.Items;

public class Part
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;

    [JsonPropertyName("payloadType")]
    public string PayloadType { get; set; } = "InlineBase64";
}
