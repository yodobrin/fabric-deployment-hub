namespace FabricDeploymentHub.Services;

public interface IPlannerService
{
    WorkspaceConfigList WorkspaceConfigs { get; }
    ItemTierConfig ItemTierConfigs { get; }
    void UpdateWorkspaceConfig(WorkspaceConfigList updatedConfig, bool saveToFile = false);
    void UpdateItemTierConfig(ItemTierConfig updatedConfig, bool saveToFile = false);
    Task<TenantDeploymentPlanResponse> PlanTenantDeploymentAsync(TenantDeploymentPlanRequest tenantRequest);
    // Task<DeploymentPlan> PlanDeploymentAsync(List<Guid> workspaceIds, List<string> modifiedFolders);
    // Task ExecuteDeploymentAsync(DeploymentPlan plan);
}
