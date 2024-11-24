namespace FabricDeploymentHub.Services;

public class PlannerService : IPlannerService
{
    private readonly ILogger<PlannerService> _logger;
    private readonly string _configurationDirectory;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public WorkspaceConfigList WorkspaceConfigs { get; private set; } = new WorkspaceConfigList();
    public ItemTierConfig ItemTierConfigs { get; private set; } = new ItemTierConfig();

    public PlannerService(ILogger<PlannerService> logger, string configurationDirectory)
    {
        _logger = logger;
        _configurationDirectory = configurationDirectory;

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        LoadConfigurations();
    }

    private void LoadConfigurations()
    {
        try
        {
            var workspaceConfigPath = Path.Combine(_configurationDirectory, "workspace-config.yml");
            var itemTierConfigPath = Path.Combine(_configurationDirectory, "item-tier-config.yml");

            // Load Workspace Configurations
            var workspaceConfigYaml = File.ReadAllText(workspaceConfigPath);
            WorkspaceConfigs = _deserializer.Deserialize<WorkspaceConfigList>(workspaceConfigYaml);

            // Load Item Tier Configurations
            var itemTierConfigYaml = File.ReadAllText(itemTierConfigPath);
            ItemTierConfigs = _deserializer.Deserialize<ItemTierConfig>(itemTierConfigYaml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configurations.");
            throw;
        }
    }

    public void UpdateWorkspaceConfig(WorkspaceConfigList updatedConfig, bool saveToFile = false)
    {
        WorkspaceConfigs = updatedConfig;

        if (saveToFile)
        {
            var workspaceConfigPath = Path.Combine(_configurationDirectory, "workspace-config.yml");
            var yaml = _serializer.Serialize(updatedConfig);
            File.WriteAllText(workspaceConfigPath, yaml);
        }
    }

    public void UpdateItemTierConfig(ItemTierConfig updatedConfig, bool saveToFile = false)
    {
        ItemTierConfigs = updatedConfig;

        if (saveToFile)
        {
            var itemTierConfigPath = Path.Combine(_configurationDirectory, "item-tier-config.yml");
            var yaml = _serializer.Serialize(updatedConfig);
            File.WriteAllText(itemTierConfigPath, yaml);
        }
    }
}