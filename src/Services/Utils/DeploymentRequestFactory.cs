namespace FabricDeploymentHub.Services.Utils;

public static class DeploymentRequestFactory
{
    public static async Task<IDeploymentRequest?> CreateDeploymentRequestAsync(
        PlatformMetadata metadata,
        string folder,
        Guid workspaceId,
        BlobContainerClient blobContainerClient,
        IDictionary<string, string> variables, // Unified variables dictionary
        ILogger logger
    )
    {
        return metadata.Metadata.Type switch
        {
            "Notebook"
                => new DeployNotebookRequest
                {
                    DisplayName = metadata.Metadata.DisplayName,
                    Description = metadata.Metadata.Description,
                    TargetWorkspaceId = workspaceId,
                    Definition = new Definition
                    {
                        Parts = await BlobUtils.GetItemPartsAsync(
                            blobContainerClient,
                            folder,
                            metadata.Metadata.Type,
                            logger,
                            variables // Pass unified variables directly
                        )
                    }
                },
            _ => null
        };
    }
}
