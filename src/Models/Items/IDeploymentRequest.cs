namespace FabricDeploymentHub.Models.Items;

public interface IDeploymentRequest
{
    string DisplayName { get; set; }
    string Description { get; set; }
    Guid TargetWorkspaceId { get; set; }
    string Validation { get; set; }
    string Type { get; }

    /// <summary>
    /// Generates the JSON-compatible payload for the deployment request.
    /// </summary>
    /// <returns>An object representing the deployment payload.</returns>
    object GeneratePayload();
    /// <summary>
    /// Creates a sanitized version of the payload for logging purposes.
    /// </summary>
    /// <returns>A sanitized object with sensitive fields redacted.</returns>
    object SanitizePayload();
}