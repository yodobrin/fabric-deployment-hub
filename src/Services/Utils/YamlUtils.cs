namespace FabricDeploymentHub.Services.Utils;

public static class YamlUtils
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static T DeserializeYaml<T>(string yamlContent)
    {
        return Deserializer.Deserialize<T>(yamlContent);
    }

    public static string SerializeToYaml<T>(T obj)
    {
        return Serializer.Serialize(obj);
    }
}