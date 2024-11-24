namespace FabricDeploymentHub.Models;
public class DeployedItem
{
    /// <summary>
    /// The unique ID of the deployed item.
    /// </summary>
    public Guid ItemId { get; set; } = Guid.Empty;

    /// <summary>
    /// The version or metadata of the deployed item, if applicable.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}