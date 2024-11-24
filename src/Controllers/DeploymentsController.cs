using Azure.Storage.Blobs;


namespace FabricDeploymentHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeploymentsController : ControllerBase
{
    private readonly IPlannerService _plannerService;
    private readonly ILogger<DeploymentsController> _logger;
    private readonly string _fabricCodeStoreUri;

    public DeploymentsController(IPlannerService plannerService, ILogger<DeploymentsController> logger, string fabricCodeStoreUri)
    {
        _plannerService = plannerService;
        _logger = logger;
         // Get the base URI for the code store from the configuration
        _fabricCodeStoreUri = fabricCodeStoreUri;
    }

    [HttpPost]
    public async Task<IActionResult> Deploy([FromBody] DeploymentRequest request)
    {
        try
        {
            _logger.LogInformation("Starting deployment for workspaces: {Workspaces}", request.WorkspaceIds);

            // Access the repository in Blob Storage
            // build the URI, take the base URI from the configuration and append the container name
            string repoContainerUrl = $"{_fabricCodeStoreUri}{request.RepoContainer}";
            _logger.LogInformation("RepoContainerUrl: {RepoContainerUrl}", repoContainerUrl);

            // Access the repository in Blob Storage
            var containerClient = new BlobContainerClient(new Uri(repoContainerUrl), new DefaultAzureCredential());


            // Get the list of files under the modified folders
            foreach (var folder in request.ModifiedFolders)
            {
                _logger.LogInformation("Processing folder: {Folder}", folder);

                var blobs = containerClient.GetBlobsAsync(prefix: folder);
                await foreach (var blobItem in blobs)
                {
                    _logger.LogInformation("Found blob: {BlobName}", blobItem.Name);

                    // Download or process the blob as needed
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var blobContent = await blobClient.DownloadContentAsync();
                    _logger.LogInformation("Blob content length: {ContentLength}", blobContent.Value.Content.ToArray().Length);
                }
            }

            // Use PlannerService to plan and execute the deployment
            // var deploymentPlan = await _plannerService.PlanDeploymentAsync(request.WorkspaceIds, request.ModifiedFolders);
            // await _plannerService.ExecuteDeploymentAsync(deploymentPlan);

            return Ok(new { message = "Deployment started successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during deployment.");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}