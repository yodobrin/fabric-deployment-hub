namespace FabricDeploymentHub.Models;

public class DeployedTypeState
{
    /// <summary>
    /// The type of items (e.g., notebook, datapipeline, dataset, etc.).
    /// </summary>
    public string ItemType { get; set; } = string.Empty;

    /// <summary>
    /// List of items of this type deployed in the workspace.
    /// </summary>
    public List<DeployedItem> Items { get; set; } = new List<DeployedItem>();
}