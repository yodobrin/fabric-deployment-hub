namespace FabricDeploymentHub.Services;

public interface IPlannerService
{
    Task<TenantDeploymentPlanResponse> PlanTenantDeploymentAsync(TenantDeploymentPlanRequest tenantRequest);
}
