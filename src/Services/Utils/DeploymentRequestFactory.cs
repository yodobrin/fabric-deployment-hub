namespace FabricDeploymentHub.Services.Utils;

public static class DeploymentRequestFactory
{
    public static IDeploymentRequest? CreateDeploymentRequest(
        PlatformMetadata metadata,
        Guid workspaceId,
        IDictionary<string, string> folderContents,
        ILogger logger
    )
    {
        try
        {
            // Ensure .platform is present
            if (!folderContents.TryGetValue(".platform", out var platformContent))
            {
                logger.LogWarning("Missing .platform metadata file in folder contents.");
                return null;
            }

            // Determine the content file name based on the item type
            var contentFileName = ItemTypeUtils.GetContentFileName(metadata.Metadata.Type);

            // Ensure the content file is present
            if (!folderContents.TryGetValue(contentFileName, out var content))
            {
                logger.LogWarning(
                    "Content file {ContentFileName} not found in folder contents.",
                    contentFileName
                );
                return null;
            }

            // Create the deployment request object based on item type
            return metadata.Metadata.Type.ToLower() switch
            {
                "notebook"
                    => new DeployNotebookRequest
                    {
                        DisplayName = metadata.Metadata.DisplayName,
                        Description = metadata.Metadata.Description,
                        TargetWorkspaceId = workspaceId,
                        Definition = new Definition
                        {
                            Parts = new List<Part>
                            {
                                new Part
                                {
                                    Path = ".platform",
                                    Payload = Convert.ToBase64String(
                                        Encoding.UTF8.GetBytes(platformContent)
                                    ),
                                    PayloadType = "InlineBase64"
                                },
                                new Part
                                {
                                    Path = contentFileName,
                                    Payload = Convert.ToBase64String(
                                        Encoding.UTF8.GetBytes(content)
                                    ),
                                    PayloadType = "InlineBase64"
                                }
                            }
                        }
                    },
                _
                    => throw new NotSupportedException(
                        $"Unsupported item type: {metadata.Metadata.Type}"
                    )
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error creating deployment request for item type {ItemType}.",
                metadata.Metadata.Type
            );
            return null;
        }
    }
}
