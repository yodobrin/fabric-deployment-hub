namespace FabricDeploymentHub.Models.Items;

public class DeployNotebookRequest : BaseDeployRequest
{
    [JsonPropertyName("definition")]
    public Definition Definition { get; set; } = new Definition();

    public override object GeneratePayload()
    {
        return new
        {
            displayName = DisplayName,
            description = Description,
            definition = new
            {
                parts = Definition.Parts.Select(part => new
                {
                    path = part.Path,
                    payload = part.Payload,
                    payloadType = part.PayloadType
                })
            }
        };
    }
}