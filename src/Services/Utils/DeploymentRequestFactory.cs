namespace FabricDeploymentHub.Services.Utils;
public static class DeploymentRequestFactory
{
public static async Task<IDeploymentRequest?> CreateDeploymentRequestAsync(PlatformMetadata metadata, string folder, Guid workspaceId, BlobContainerClient blobContainerClient,ILogger logger)
{
    return metadata.Metadata.Type switch
    {
        "Notebook" => new DeployNotebookRequest
        {
            DisplayName = metadata.Metadata.DisplayName,
            Description = metadata.Metadata.Description,
            TargetWorkspaceId = workspaceId,
            Definition = new Definition
            {
                Parts = await BlobUtils.GetNotebookPartsAsync(blobContainerClient, folder, logger)
            }
        },
        _ => null
    };
}
}