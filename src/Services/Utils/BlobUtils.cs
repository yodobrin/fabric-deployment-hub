namespace FabricDeploymentHub.Services.Utils;

public static class BlobUtils
{
    public static BlobContainerClient GetContainerClient(BlobServiceClient blobServiceClient, string containerName)
    {
        return blobServiceClient.GetBlobContainerClient(containerName);
    }

    public static async Task<string> DownloadBlobContentAsync(BlobContainerClient containerClient, string blobName)
    {
        var blobClient = containerClient.GetBlobClient(blobName);
        return await DownloadBlobContentAsync(blobClient);
    }

    public static async Task UploadBlobContentAsync(BlobContainerClient containerClient, string blobName, string content)
    {
        var blobClient = containerClient.GetBlobClient(blobName);
        await UploadBlobContentAsync(blobClient, content);
    }

    public static async Task<PlatformMetadata?> ParsePlatformMetadataAsync(BlobContainerClient blobContainerClient, string folder, ILogger logger)
    {
        try
        {
            var platformBlobPath = $"{folder}/.platform";
            var platformContent = await TryDownloadBlobContentAsync(blobContainerClient, platformBlobPath, logger);

            if (platformContent == null)
            {
                logger.LogWarning("Platform metadata file not found in folder {Folder}.", folder);
                return null;
            }

            return JsonSerializer.Deserialize<PlatformMetadata>(platformContent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse platform metadata for folder {Folder}.", folder);
            return null;
        }
    }

    public static async Task<List<Part>> GetNotebookPartsAsync(BlobContainerClient blobContainerClient, string folder, ILogger logger)
    {
        var notebookParts = new List<Part>();

        // Define the required files for a notebook
        var requiredFiles = new[] { ".platform", "notebook-content.py" };

        foreach (var fileName in requiredFiles)
        {
            var blobPath = $"{folder}/{fileName}";

            try
            {
                logger.LogInformation("BlobUtils:GetNotebookPartsAsync| Checking for blob: {BlobPath}", blobPath);

                var blobContent = await TryDownloadBlobContentAsync(blobContainerClient, blobPath, logger);

                if (blobContent != null)
                {
                    logger.LogInformation($"BlobUtils:GetNotebookPartsAsync| content is not null, content length={blobContent.Length}");
                    notebookParts.Add(new Part
                    {
                        Path = fileName,
                        Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(blobContent)),
                        PayloadType = "InlineBase64"
                    });

                    logger.LogInformation("BlobUtils:GetNotebookPartsAsync| Successfully added blob content for {BlobPath}.", blobPath);
                }
                else
                {
                    logger.LogWarning("BlobUtils:GetNotebookPartsAsync|Blob {BlobPath} does not exist.", blobPath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BlobUtils:GetNotebookPartsAsync| Error processing blob {BlobPath}.", blobPath);
            }
        }

        return notebookParts;
    }

    // Private helper methods

    private static async Task<string?> TryDownloadBlobContentAsync(BlobContainerClient containerClient, string blobName, ILogger? logger = null)
    {
        try
        {
            var blobClient = containerClient.GetBlobClient(blobName);

            if (await blobClient.ExistsAsync())
            {
                return await DownloadBlobContentAsync(blobClient);
            }
            else
            {
                logger?.LogWarning("Blob {BlobName} does not exist.", blobName);
                return null;
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to download content for blob {BlobName}.", blobName);
            return null;
        }
    }

    private static async Task<string> DownloadBlobContentAsync(BlobClient blobClient)
    {
        var blobContent = await blobClient.DownloadContentAsync();
        return blobContent.Value.Content.ToString();
    }

    private static async Task UploadBlobContentAsync(BlobClient blobClient, string content)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    public static async Task<TenantDeploymentResponse> SaveDeploymentPlanToBlobAsync(
    BlobServiceClient blobServiceClient,
    TenantDeploymentResponse response,
    string repoContainerName,
    ILogger logger)
    {
        // Generate the container name for the deployment plan
        var deploymentPlanContainerName = $"{repoContainerName}-deployment-plan";

        // Ensure the container name does not exceed 63 characters
        if (deploymentPlanContainerName.Length > 63)
        {
            deploymentPlanContainerName = $"{Guid.NewGuid()}-deployment-plan";
            logger.LogWarning("BlobUtils:SaveDeploymentPlanToBlobAsync| Generated container name exceeded length limit. Using GUID-based name: {ContainerName}", deploymentPlanContainerName);
        }

        try
        {
            // Create or get the deployment plan container
            var containerClient = blobServiceClient.GetBlobContainerClient(deploymentPlanContainerName);
            await containerClient.CreateIfNotExistsAsync();
            logger.LogInformation("BlobUtils:SaveDeploymentPlanToBlobAsync| Created or accessed deployment plan container: {ContainerName}", deploymentPlanContainerName);

            // Iterate through the workspaces in the response
            foreach (var workspace in response.Workspaces)
            {
                var workspaceFolder = $"{workspace.WorkspaceId}";

                // Iterate through deployment requests and organize by type
                foreach (var request in workspace.DeploymentRequests)
                {
                    var typeFolder = request.GetType().Name; // Use the class name as the type folder
                    var blobPath = $"{workspaceFolder}/{typeFolder}/{Guid.NewGuid()}.json";

                    // Serialize the payload for saving
                    var payload = JsonSerializer.Serialize(request.GeneratePayload());
                    logger.LogInformation("BlobUtils:SaveDeploymentPlanToBlobAsync| Saving deployment request for workspace {WorkspaceId} to blob {BlobPath}", workspace.WorkspaceId, blobPath);
                    await UploadBlobContentAsync(containerClient, blobPath, payload);
                    
                }
            }

            // Add metadata about the saved plan to the response
            response.SavedContainerName = deploymentPlanContainerName;
            response.SavedTimestamp = DateTime.UtcNow;

            logger.LogInformation("BlobUtils:SaveDeploymentPlanToBlobAsync| Deployment plan saved successfully to container: {ContainerName}", deploymentPlanContainerName);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlobUtils:SaveDeploymentPlanToBlobAsync| Failed to save deployment plan to Blob Storage.");
            throw;
        }
    }
}