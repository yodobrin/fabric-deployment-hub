using System.Text.Json.Serialization;

namespace FabricDeploymentHub.Models.Items;

public class DeployNotebookRequest : BaseDeployRequest
{
    [JsonPropertyName("definition")]
    public Definition Definition { get; set; } = new Definition();

    [JsonPropertyName("type")]
    public override string Type => "notebook";

    public override object GeneratePayload()
    {
        // Generate the payload matching the expected structure
        return new
        {
            displayName = DisplayName,
            description = Description,
            id = Id,
            type = Type,
            validation = Validation,
            definition = new
            {
                parts = Definition.Parts.Select(
                    part =>
                        new
                        {
                            path = part.Path,
                            payload = part.Payload,
                            payloadType = part.PayloadType
                        }
                )
            }
        };
    }

    public override object SanitizePayload()
    {
        return new
        {
            displayName = DisplayName,
            description = Description,
            id = Id,
            type = Type,
            validation = Validation,
            definition = new
            {
                parts = Definition.Parts.Select(
                    part =>
                        new
                        {
                            path = part.Path,
                            payload = "<redacted>",
                            payloadType = part.PayloadType
                        }
                )
            }
        };
    }
}
