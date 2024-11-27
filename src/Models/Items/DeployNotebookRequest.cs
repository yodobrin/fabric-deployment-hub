using System.Text.Json.Serialization;

namespace FabricDeploymentHub.Models.Items;

public class DeployNotebookRequest : BaseDeployRequest
{
    [JsonPropertyName("definition")]
    public Definition Definition { get; set; } = new Definition();

    public override object GeneratePayload()
    {
        // Generate the payload matching the expected structure
        return new
        {
            displayName = DisplayName, // Correct JSON property name
            description = Description, // Correct JSON property name
            definition = new
            {
                parts = Definition.Parts.Select(part => new
                {
                    path = part.Path, // Matches the expected property name
                    payload = part.Payload, // Matches the expected property name
                    payloadType = part.PayloadType // Matches the expected property name
                })
            }
        };
    }
}