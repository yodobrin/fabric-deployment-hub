namespace FabricDeploymentHub.Services.Binding;

public class DependencyBindingService : IDependencyBindingService
{
    private readonly IFabricRestService _fabricRestService;
    private readonly ILogger<DependencyBindingService> _logger;

    public DependencyBindingService(
        IFabricRestService fabricRestService,
        ILogger<DependencyBindingService> logger
    )
    {
        _fabricRestService = fabricRestService;
        _logger = logger;
    }

    public async Task<(bool Found, Guid DependencyId, string Description)> FindDependencyIdAsync(
        DependencyType dependencyType,
        string dependencyName,
        Guid workspaceId
    )
    {
        // Determine the correct URI for the dependency type
        string uri = dependencyType switch
        {
            DependencyType.Lakehouse => $"/v1/workspaces/{workspaceId}/lakehouses",
            DependencyType.Environment => $"/v1/workspaces/{workspaceId}/environments",
            _
                => throw new NotSupportedException($"Dependency type '{dependencyType}' is not supported." )
        };

        _logger.LogInformation($"Fetching dependencies of type '{dependencyType}' from URI: {uri}");

        try
        {
            // Fetch response from API
            var response = await _fabricRestService.GetAsync(uri);

            // Deserialize based on dependency type
            return dependencyType switch
            {
                DependencyType.Lakehouse => DeserializeAndFindLakehouse(response, dependencyName),
                DependencyType.Environment => DeserializeAndFindEnvironment(response, dependencyName),
                _ => (false, Guid.Empty, "Unsupported dependency type") // Default case for unsupported types
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                $"Error fetching dependencies of type '{dependencyType}' with name {dependencyName} from workspace {workspaceId}."
            );
            throw new InvalidOperationException(
                $"Failed to fetch dependencies of type '{dependencyType}' with name {dependencyName} for workspace {workspaceId}.",
                ex
            );
        }
    }

    private (bool Found, Guid DependencyId, string Description) DeserializeAndFindLakehouse(
        string response,
        string dependencyName
    )
    {
        var lakehouses = JsonSerializer.Deserialize<LakehouseResponse>(response);

        if (lakehouses == null || lakehouses.Value == null || !lakehouses.Value.Any())
        {
            _logger.LogWarning($"No lakehouses found in the response.");
            return (false, Guid.Empty, "No lakehouses found.");
        }
        var match = lakehouses.Value.FirstOrDefault(
            l => l.DisplayName.Equals(dependencyName, StringComparison.OrdinalIgnoreCase) );
        return match != null
            ? (true, Guid.Parse(match.Id), "Lakehouse found.")
            : (false, Guid.Empty, "Lakehouse not found.");
    }

    private (bool Found, Guid DependencyId, string Description) DeserializeAndFindEnvironment(
        string response,
        string dependencyName
    )
    {
        var environments = JsonSerializer.Deserialize<EnvironmentResponse>(response);

        if (environments == null || environments.Value == null || !environments.Value.Any())
        {
            _logger.LogWarning($"No environments found in the response.");
            return (false, Guid.Empty, "No environments found.");
        }

        var match = environments.Value.FirstOrDefault(
            e => e.DisplayName.Equals(dependencyName, StringComparison.OrdinalIgnoreCase)
        );

        return match != null
                ? (true, Guid.Parse(match.Id), "Environment found.")
                : (false, Guid.Empty, "Environment not found.");
    }
}
