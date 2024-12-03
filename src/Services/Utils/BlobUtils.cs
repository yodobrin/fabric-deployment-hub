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

    public static async Task<TenantDeploymentPlanResponse?> LoadDeploymentPlanFromBlobAsync(
        BlobServiceClient blobServiceClient,
        string containerName,
        string fileName,
        ILogger logger)
    {
        try
        {
            // Get the container client
            var containerClient = GetContainerClient(blobServiceClient, containerName);

            // Ensure the file exists
            var blobClient = containerClient.GetBlobClient(fileName);
            if (!await blobClient.ExistsAsync())
            {
                logger.LogWarning("BlobUtils:LoadDeploymentPlanFromBlobAsync| File {FileName} does not exist in container {ContainerName}.", fileName, containerName);
                return null;
            }

            // Download and deserialize the blob content
            var content = await DownloadBlobContentAsync(blobClient);
            var deploymentPlan = JsonSerializer.Deserialize<TenantDeploymentPlanResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // To handle potential case mismatches in serialized JSON
            });

            if (deploymentPlan == null)
            {
                logger.LogWarning("BlobUtils:LoadDeploymentPlanFromBlobAsync| Failed to deserialize deployment plan from blob {FileName}.", fileName);
                return null;
            }

            logger.LogInformation("BlobUtils:LoadDeploymentPlanFromBlobAsync| Successfully loaded deployment plan from {FileName}.", fileName);
            return deploymentPlan;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlobUtils:LoadDeploymentPlanFromBlobAsync| Error loading deployment plan from blob {FileName}.", fileName);
            throw;
        }
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

    public static async Task<TenantDeploymentPlanResponse> SaveDeploymentPlanToBlobAsync(
    BlobServiceClient blobServiceClient,
    TenantDeploymentPlanResponse response,
    ILogger logger)
    {
        try
        {
            // Create or get the deployment plan container
            var containerClient = blobServiceClient.GetBlobContainerClient(response.SavedContainerName);
            await containerClient.CreateIfNotExistsAsync();
            logger.LogInformation($"BlobUtils:SaveDeploymentPlanToBlobAsync| Created or accessed deployment plan in container: {response.SavedContainerName}, plan file: {response.SavedPlanName}");
        
            // Transform the response object for serialization
            var transformedResponse = new
            {
                workspaces = response.Workspaces.Select(workspace => new
                {
                    workspaceId = workspace.WorkspaceId,
                    deploymentRequests = workspace.DeploymentRequests.Select(request => request.GeneratePayload()),
                    issues = workspace.Issues,
                    hasErrors = workspace.HasErrors,
                    messages = workspace.Messages
                }),
                issues = response.Issues,
                hasErrors = response.HasErrors,
                savedContainerName = response.SavedContainerName,
                savedPlanName = response.SavedPlanName ?? $"tenant-plan-{DateTime.UtcNow:yyyyMMddHHmmss}.json",
                savedTimestamp = response.SavedTimestamp ?? DateTime.UtcNow,
                messages = response.Messages
            };

            // Serialize the transformed response
            var serializedResponse = JsonSerializer.Serialize(transformedResponse, new JsonSerializerOptions { WriteIndented = true });

            // Save the serialized plan to Blob Storage
            var savedPlanName = transformedResponse.savedPlanName;
            await UploadBlobContentAsync(containerClient, savedPlanName, serializedResponse);
            logger.LogInformation("BlobUtils:SaveDeploymentPlanToBlobAsync| Deployment plan saved successfully to blob: {BlobName}", savedPlanName);

            // Update the response object with the saved plan details
            response.SavedPlanName = savedPlanName;
            response.SavedTimestamp = transformedResponse.savedTimestamp;

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlobUtils:SaveDeploymentPlanToBlobAsync| Failed to save deployment plan to Blob Storage.");
            throw;
        }
    }
}