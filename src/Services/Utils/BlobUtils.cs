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
    public static async Task SaveValidatedPlanToBlobAsync(
        BlobServiceClient blobServiceClient,
        TenantDeploymentPlanResponse deploymentPlan,
        string containerName,
        string fileName,
        ILogger logger)
    {
        await SavePlanToBlobAsync(
            blobServiceClient,
            deploymentPlan,
            containerName,
            fileName,
            logger,
            plan => new
            {
                workspaces = plan.Workspaces.Select(workspace => new
                {
                    workspaceId = workspace.WorkspaceId,
                    deploymentRequests = workspace.DeploymentRequests.Select(request => request.GeneratePayload()), // Use GeneratePayload here
                    issues = workspace.Issues,
                    hasErrors = workspace.HasErrors,
                    messages = workspace.Messages
                }),
                issues = plan.Issues,
                hasErrors = plan.HasErrors,
                savedContainerName = containerName,
                savedPlanName = fileName,
                savedTimestamp = DateTime.UtcNow,
                messages = plan.Messages
            }
        );

        logger.LogInformation("Validated deployment plan saved successfully to blob: {BlobName} in container {ContainerName}.", fileName, containerName);
    }

    public static async Task<TenantDeploymentPlanResponse> SaveDeploymentPlanToBlobAsync(
        BlobServiceClient blobServiceClient,
        TenantDeploymentPlanResponse response,
        string containerName,
        string fileName,
        ILogger logger)
    {
        await SavePlanToBlobAsync(
            blobServiceClient,
            response,
            containerName,
            fileName,
            logger,
            plan => new
            {
                workspaces = plan.Workspaces.Select(workspace => new
                {
                    workspaceId = workspace.WorkspaceId,
                    deploymentRequests = workspace.DeploymentRequests.Select(request => request.GeneratePayload()),
                    issues = workspace.Issues,
                    hasErrors = workspace.HasErrors,
                    messages = workspace.Messages
                }),
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
                PropertyNameCaseInsensitive = true, // To handle potential case mismatches in serialized JSON
                Converters = { new DeploymentRequestConverter() }
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
    private static async Task SavePlanToBlobAsync(
        BlobServiceClient blobServiceClient,
        TenantDeploymentPlanResponse deploymentPlan,
        string containerName,
        string fileName,
        ILogger logger,
        Func<TenantDeploymentPlanResponse, object> transformResponse)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            // Transform the response object for serialization
            var transformedResponse = transformResponse(deploymentPlan);

            // Serialize the transformed response
            var serializedPlan = JsonSerializer.Serialize(transformedResponse, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null // Preserve original property names
            });

            await UploadBlobContentAsync(containerClient, fileName, serializedPlan);

            logger.LogInformation("Deployment plan saved to blob: {BlobName} in container: {ContainerName}.", fileName, containerName);
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