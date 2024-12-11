namespace FabricDeploymentHub.Services.Utils;

public class DeploymentRequestConverter : JsonConverter<IDeploymentRequest>
{
    public override IDeploymentRequest? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var jsonDocument = JsonDocument.ParseValue(ref reader);
        var jsonObject = jsonDocument.RootElement;

        if (!jsonObject.TryGetProperty("type", out var typeProperty))
        {
            throw new JsonException("Missing 'type' property in deployment request.");
        }

        var type = typeProperty.GetString();
        return type switch
        {
            "notebook"
                => JsonSerializer.Deserialize<DeployNotebookRequest>(
                    jsonObject.GetRawText(),
                    options
                ),
            // Add other types here...
            _ => throw new NotSupportedException($"Unknown deployment request type: {type}")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        IDeploymentRequest value,
        JsonSerializerOptions options
    )
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
