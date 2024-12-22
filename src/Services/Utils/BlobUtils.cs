namespace FabricDeploymentHub.Services.Utils;

public static class BlobUtils
{
    public static BlobContainerClient GetContainerClient(
        BlobServiceClient blobServiceClient,
        string containerName
    )
    {
        return blobServiceClient.GetBlobContainerClient(containerName);
    }

    public static async Task<string> DownloadBlobContentAsync(
        BlobContainerClient containerClient,
        string blobName
    )
    {
        var blobClient = containerClient.GetBlobClient(blobName);
        return await DownloadBlobContentAsync(blobClient);
    }

    public static async Task UploadBlobContentAsync(
        BlobContainerClient containerClient,
        string blobName,
        string content
    )
    {
        var blobClient = containerClient.GetBlobClient(blobName);
        await UploadBlobContentAsync(blobClient, content);
    }

    public static async Task SaveValidatedPlanToBlobAsync(
        BlobServiceClient blobServiceClient,
        TenantDeploymentPlanResponse deploymentPlan,
        string containerName,
        string fileName,
        ILogger logger
    )
    {
        await SavePlanToBlobAsync(
            blobServiceClient,
            deploymentPlan,
            containerName,
            fileName,
            logger,
            plan =>
                new
                {
                    workspaces = plan.Workspaces.Select(
                        workspace =>
                            new
                            {
                                workspaceId = workspace.WorkspaceId,
                                deploymentRequests = workspace.DeploymentRequests.Select(
                                    request => request.GeneratePayload()
                                ), // Use GeneratePayload here
                                issues = workspace.Issues,
                                hasErrors = workspace.HasErrors,
                                messages = workspace.Messages
                            }
                    ),
                    issues = plan.Issues,
                    hasErrors = plan.HasErrors,
                    savedContainerName = containerName,
                    savedPlanName = fileName,
                    savedTimestamp = DateTime.UtcNow,
                    messages = plan.Messages
                }
        );

        logger.LogInformation(
            "Validated deployment plan saved successfully to blob: {BlobName} in container {ContainerName}.",
            fileName,
            containerName
        );
    }

    public static async Task<TenantDeploymentPlanResponse> SaveDeploymentPlanToBlobAsync(
        BlobServiceClient blobServiceClient,
        TenantDeploymentPlanResponse response,
        string containerName,
        string fileName,
        ILogger logger
    )
    {
        await SavePlanToBlobAsync(
            blobServiceClient,
            response,
            containerName,
            fileName,
            logger,
            plan =>
                new
                {
                    workspaces = plan.Workspaces.Select(
                        workspace =>
                            new
                            {
                                workspaceId = workspace.WorkspaceId,
                                deploymentRequests = workspace.DeploymentRequests.Select(
                                    request => request.GeneratePayload()
                                ),
                                issues = workspace.Issues,
                                hasErrors = workspace.HasErrors,
                                messages = workspace.Messages
                            }
                    ),
                    issues = plan.Issues,
                    hasErrors = plan.HasErrors,
                    savedContainerName = containerName,
                    savedPlanName = fileName,
                    savedTimestamp = DateTime.UtcNow,
                    messages = plan.Messages
                }
        );

        // Update the original response object with the saved details
        response.SavedPlanName = fileName;
        response.SavedTimestamp = DateTime.UtcNow;

        return response;
    }

    public static async Task<Dictionary<string, string>> GetFolderContentsAsync(
        BlobContainerClient blobContainerClient,
        string folder,
        ILogger logger
    )
    {
        var folderContents = new Dictionary<string, string>();

        try
        {
            logger.LogInformation("Fetching contents of folder: {Folder}", folder);

            await foreach (var blobItem in blobContainerClient.GetBlobsAsync(prefix: folder + "/"))
            {
                var blobPath = blobItem.Name;
                // Extract the last part of the blob path (after the last '/') this would be used as the key in the dictionary
                // This might be not valid for scenarios with sub folders which might be required to be added as payload - TBD.
                var contentKey = blobPath.Contains('/')
                    ? blobPath.Substring(blobPath.LastIndexOf('/') + 1)
                    : blobPath;

                var contentLength = blobItem.Properties?.ContentLength ?? 0;

                // Skip blobs with ContentLength of 0 - in most scenarios these are folders.
                if (contentLength == 0)
                {
                    logger.LogWarning(
                        "Skipping read for blob {BlobPath} as its ContentLength is 0.",
                        blobPath
                    );
                    continue; // Skip this item
                }

                logger.LogInformation(
                    "Found blob: {BlobPath}, with type {BlobContentLength} registering in dict with key {ContentKey}",
                    blobPath,
                    contentLength,
                    contentKey
                );

                try
                {
                    var content = await TryDownloadBlobContentAsync(
                        blobContainerClient,
                        blobPath,
                        logger
                    );

                    if (content != null)
                    {
                        folderContents[contentKey] = content;
                        logger.LogInformation(
                            "Successfully read content for blob: {BlobPath}",
                            blobPath
                        );
                    }
                    else
                    {
                        logger.LogWarning("Failed to read content for blob: {BlobPath}", blobPath);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error reading content for blob: {BlobPath}", blobPath);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch contents of folder: {Folder}", folder);
            throw;
        }

        return folderContents;
    }

    public static async Task<TenantDeploymentPlanResponse?> LoadDeploymentPlanFromBlobAsync(
        BlobServiceClient blobServiceClient,
        string containerName,
        string fileName,
        ILogger logger
    )
    {
        try
        {
            // Get the container client
            var containerClient = GetContainerClient(blobServiceClient, containerName);

            // Ensure the file exists
            var blobClient = containerClient.GetBlobClient(fileName);
            if (!await blobClient.ExistsAsync())
            {
                logger.LogWarning(
                    "BlobUtils:LoadDeploymentPlanFromBlobAsync| File {FileName} does not exist in container {ContainerName}.",
                    fileName,
                    containerName
                );
                return null;
            }

            // Download and deserialize the blob content
            var content = await DownloadBlobContentAsync(blobClient);
            var deploymentPlan = JsonSerializer.Deserialize<TenantDeploymentPlanResponse>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // To handle potential case mismatches in serialized JSON
                    Converters = { new DeploymentRequestConverter() }
                }
            );

            if (deploymentPlan == null)
            {
                logger.LogWarning(
                    "BlobUtils:LoadDeploymentPlanFromBlobAsync| Failed to deserialize deployment plan from blob {FileName}.",
                    fileName
                );
                return null;
            }

            logger.LogInformation(
                "BlobUtils:LoadDeploymentPlanFromBlobAsync| Successfully loaded deployment plan from {FileName}.",
                fileName
            );
            return deploymentPlan;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "BlobUtils:LoadDeploymentPlanFromBlobAsync| Error loading deployment plan from blob {FileName}.",
                fileName
            );
            throw;
        }
    }

    private static async Task<string?> TryDownloadBlobContentAsync(
        BlobContainerClient containerClient,
        string blobName,
        ILogger? logger = null
    )
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

    private static async Task SavePlanToBlobAsync(
        BlobServiceClient blobServiceClient,
        TenantDeploymentPlanResponse deploymentPlan,
        string containerName,
        string fileName,
        ILogger logger,
        Func<TenantDeploymentPlanResponse, object> transformResponse
    )
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            // Transform the response object for serialization
            var transformedResponse = transformResponse(deploymentPlan);

            // Serialize the transformed response
            var serializedPlan = JsonSerializer.Serialize(
                transformedResponse,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null // Preserve original property names
                }
            );

            await UploadBlobContentAsync(containerClient, fileName, serializedPlan);

            logger.LogInformation(
                "Deployment plan saved to blob: {BlobName} in container: {ContainerName}.",
                fileName,
                containerName
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save deployment plan to blob.");
            throw;
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
}
