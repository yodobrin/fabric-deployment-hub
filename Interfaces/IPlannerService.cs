namespace FabricDeploymentHub.Services;

public interface IPlannerService
{
    WorkspaceConfigList WorkspaceConfigs { get; }
    ItemTierConfig ItemTierConfigs { get; }
    void UpdateWorkspaceConfig(WorkspaceConfigList updatedConfig, bool saveToFile = false);
    void UpdateItemTierConfig(ItemTierConfig updatedConfig, bool saveToFile = false);
}
