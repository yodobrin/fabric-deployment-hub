
namespace FabricDeploymentHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeploymentsController : ControllerBase
{
    private readonly IPlannerService _plannerService;
    private readonly ILogger<DeploymentsController> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    private readonly DeploymentProcessor _deploymentProcessor;

    public DeploymentsController(IPlannerService plannerService, ILogger<DeploymentsController> logger, BlobServiceClient blobServiceClient, DeploymentProcessor deploymentProcessor)
    {
        _plannerService = plannerService;
        _logger = logger;
        _blobServiceClient = blobServiceClient;
        _deploymentProcessor = deploymentProcessor;
    }

    [HttpPost]
    public async Task<IActionResult> Deploy([FromBody] TenantDeploymentRequest request)
    {
        try
        {
            _logger.LogInformation("Starting deployment for workspaces: {Workspaces}", request.WorkspaceIds);

            // Access the repository container
            var containerClient = _blobServiceClient.GetBlobContainerClient(request.RepoContainer);
            _logger.LogInformation("Accessing RepoContainer: {RepoContainer}", request.RepoContainer);

            // Get the list of files under the modified folders | for now its tailored for notebook |
            // var requiredFiles = new List<string> { ".platform", "notebook-content.py" };
            var deploymentRequests = new List<IDeploymentRequest>();
            foreach (var folder in request.ModifiedFolders)
            {
                _logger.LogInformation("Processing folder: {Folder}", folder);
                // understand the current folder content - looking at the meta-data
                var platformBlob = containerClient.GetBlobClient($"{folder}/.platform");
                var platformContent = await platformBlob.DownloadContentAsync();
                var platformJson = JsonSerializer.Deserialize<PlatformMetadata>( platformContent.Value.Content.ToArray());


                if (platformJson?.Metadata.Type == "Notebook")
                {
                    var deployNotebookRequest = new DeployNotebookRequest
                    {
                        DisplayName = platformJson.Metadata.DisplayName,
                        Description = platformJson.Metadata.Description,
                        TargetWorkspaceId = request.WorkspaceIds.First() // this would have to change to support multiple workspaces
                    };

                    var requiredFiles = new List<string> { ".platform", "notebook-content.py" };

                    foreach (var requiredFile in requiredFiles)
                    {
                        var blobClient = containerClient.GetBlobClient($"{folder}/{requiredFile}");
                        var blobContent = await blobClient.DownloadContentAsync();

                        deployNotebookRequest.Definition.Parts.Add(new Part
                        {
                            Path = requiredFile,
                            Payload = Convert.ToBase64String(blobContent.Value.Content.ToArray())
                        });
                    }

                    deploymentRequests.Add(deployNotebookRequest);
                }

            }

            // Use PlannerService to plan and execute the deployment
            // var deploymentPlan = await _plannerService.PlanDeploymentAsync(request.WorkspaceIds, request.ModifiedFolders);
            // await _plannerService.ExecuteDeploymentAsync(deploymentPlan);

                var tasks = deploymentRequests.Select(async deploymentRequest =>
                {
                    try
                    {
                        var response = await _deploymentProcessor.SendDeploymentRequestAsync(deploymentRequest);
                        _logger.LogInformation("Deployment completed: {Response}", response);

                        var payload = deploymentRequest.GeneratePayload();
                        _logger.LogInformation("Generated Payload: {Payload}", JsonSerializer.Serialize(payload));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing deployment request for: {DeploymentRequest}", JsonSerializer.Serialize(deploymentRequest));
                    }
                });

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);

                _logger.LogInformation("All deployment requests have been processed.");

                return Ok(new { message = "Deployment requests processed successfully." });
            }catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during deployment.");
                    return StatusCode(500, new { error = ex.Message });
                }
    }
    // private async Task SendDeploymentRequestAsync(object payload)
    // {
    //     // Simulate sending the payload to the deployment API
    //     _logger.LogInformation("Sending payload: {Payload}", JsonSerializer.Serialize(payload));
    //     await Task.CompletedTask;
    // }
}