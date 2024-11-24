public class WorkspaceStateService : IWorkspaceStateService
{
    private readonly FabricClient _fabricClient;

    private static readonly List<string> SupportedItemTypes = new()
    {
        "notebook",
        "datapipeline",
        "dataset",
        "report"
    };

    public WorkspaceStateService()
    {
        var credential = new DefaultAzureCredential();
        //var accessToken = credential.GetToken(new Azure.Core.TokenRequestContext(new[] { "https://fabric.microsoft.com/.default" })).Token;
        var accessToken = "tbd";
        // var credential = new DefaultAzureCredential();
        _fabricClient = new FabricClient(accessToken);
    }

    public async Task<DeployedWorkspaceState> GetWorkspaceStateAsync(Guid workspaceId)
    {
        var items = new List<CoreModels.Item>();

        try
        {
            // Enumerate the async response
            await foreach (var item in _fabricClient.Core.Items.ListItemsAsync(workspaceId))
            {
                if (item != null)
                {
                    items.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving items for workspace {workspaceId}: {ex.Message}", ex);
        }

        // Ensure all supported item types are included in the output
        var groupedItems = items.GroupBy(item => item.Type)
            .ToDictionary(group => group.Key, group => group.ToList());

        var itemStates = SupportedItemTypes.Select(type =>
        {
            groupedItems.TryGetValue(type, out var itemsOfType);
            return new DeployedTypeState
            {
                ItemType = type,
                Items = itemsOfType?.Select(item => new DeployedItem
                {
                    ItemId = item.Id ?? Guid.Empty,
                    DisplayName = item.DisplayName ?? string.Empty
                }).ToList() ?? new List<DeployedItem>()
            };
        }).ToList();

        return new DeployedWorkspaceState
        {
            WorkspaceId = workspaceId,
            ItemStates = itemStates
        };
    }
}