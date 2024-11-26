namespace FabricDeploymentHub.Models.Items;

public interface IDeploymentRequest
{
    string DisplayName { get; set; }
    string Description { get; set; }
    Guid TargetWorkspaceId { get; set; }

    /// <summary>
    /// Generates the JSON-compatible payload for the deployment request.
    /// </summary>
    /// <returns>An object representing the deployment payload.</returns>
    object GeneratePayload();
}