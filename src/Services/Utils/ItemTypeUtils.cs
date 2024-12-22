namespace FabricDeploymentHub.Services.Utils;

public static class ItemTypeUtils
{
    public static string GetContentFileName(string itemType)
    {
        return itemType.ToLower() switch
        {
            "notebook" => "notebook-content.py",
            "lakehouse" => "lakehouse.metadata.json",
            "pipeline" => "pipeline-content.json",
            _ => throw new NotSupportedException($"Unsupported item type: {itemType}")
        };
    }
}
