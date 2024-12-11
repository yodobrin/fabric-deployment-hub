namespace FabricDeploymentHub.Services.Utils;

public static class ValidationUtils
{
    public static List<string> ValidateTenantDeploymentRequest(
        TenantDeploymentPlanRequest tenantRequest
    )
    {
        var issues = new List<string>();

        if (tenantRequest == null)
        {
            issues.Add("TenantDeploymentRequest is null.");
            return issues;
        }

        // if (tenantRequest.WorkspaceIds == null || !tenantRequest.WorkspaceIds.Any())
        // {
        //     issues.Add("No workspace IDs provided in the request.");
        // }

        if (tenantRequest.ModifiedFolders == null || !tenantRequest.ModifiedFolders.Any())
        {
            issues.Add("No modified folders provided in the request.");
        }

        return issues;
    }
}
