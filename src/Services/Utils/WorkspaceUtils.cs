namespace FabricDeploymentHub.Services.Utils;

public static class WorkspaceUtils
{
    public static bool IsEligibleForDeployment(
        PlatformMetadata platformMetadata,
        Guid workspaceId,
        WorkspaceConfig workspaceConfig,
        ItemTierConfig itemTierConfigs,
        ILogger logger
    )
    {
        if (workspaceConfig == null)
        {
            logger.LogWarning(
                "Workspace ID {WorkspaceId} not found in configurations.",
                workspaceId
            );
            return false;
        }

        var tier = workspaceConfig.Tier;
        if (string.IsNullOrEmpty(tier))
        {
            logger.LogWarning(
                "Workspace ID {WorkspaceId} does not have a valid tier.",
                workspaceId
            );
            return false;
        }

        if (!itemTierConfigs.Tiers.TryGetValue(tier, out var tierConfig))
        {
            logger.LogWarning("Tier {Tier} is not defined in the item tier configurations.", tier);
            return false;
        }

        logger.LogInformation(
            "Checking eligibility of item {ItemName} of type {Type} for deployment to workspace {WorkspaceId}.",
            platformMetadata.Metadata.DisplayName,
            platformMetadata.Metadata.Type,
            workspaceId
        );

        // Log allowed item types for the tier
        logger.LogInformation(
            "Allowed item types for tier {Tier}: {AllowedItemTypes}",
            tier,
            string.Join(", ", tierConfig.Items.Keys)
        );

        if (!tierConfig.Items.TryGetValue(platformMetadata.Metadata.Type, out var allowedItems))
        {
            logger.LogInformation(
                "Item type {Type} is not allowed in tier {Tier}.",
                platformMetadata.Metadata.Type,
                tier
            );
            return false;
        }

        if (allowedItems.Contains(platformMetadata.Metadata.DisplayName))
        {
            logger.LogInformation(
                "Item {ItemName} of type {Type} is eligible for deployment to workspace {WorkspaceId}.",
                platformMetadata.Metadata.DisplayName,
                platformMetadata.Metadata.Type,
                workspaceId
            );
            return true;
        }

        logger.LogInformation(
            "Item {ItemName} of type {Type} is not eligible for deployment to workspace {WorkspaceId}.",
            platformMetadata.Metadata.DisplayName,
            platformMetadata.Metadata.Type,
            workspaceId
        );
        return false;
    }

    public static bool IsEligibleForDeployment(
        PlatformMetadata platformMetadata,
        Guid workspaceId,
        WorkspaceConfigList workspaceConfigs,
        ItemTierConfig itemTierConfigs,
        ILogger logger
    )
    {
        var workspaceConfig = GetWorkspaceConfig(workspaceConfigs, workspaceId, logger);
        if (workspaceConfig == null)
            return false;

        var tier = workspaceConfig.Tier;
        if (string.IsNullOrEmpty(tier))
        {
            logger.LogWarning(
                "Workspace ID {WorkspaceId} does not have a valid tier.",
                workspaceId
            );
            return false;
        }

        if (!itemTierConfigs.Tiers.TryGetValue(tier, out var tierConfig))
        {
            logger.LogWarning("Tier {Tier} is not defined in the item tier configurations.", tier);
            return false;
        }
        logger.LogInformation(
            "Checking eligibility of item {ItemName} of type {Type} for deployment to workspace {WorkspaceId}.",
            platformMetadata.Metadata.DisplayName,
            platformMetadata.Metadata.Type,
            workspaceId
        );
        // print the allowed items for the tier
        logger.LogInformation(
            "Allowed item types for tier {Tier}: {AllowedItemTypes}",
            tier,
            string.Join(", ", tierConfig.Items.Keys)
        );
        if (!tierConfig.Items.TryGetValue(platformMetadata.Metadata.Type, out var allowedItems))
        {
            logger.LogInformation(
                "Item type {Type} is not allowed in tier {Tier}.",
                platformMetadata.Metadata.Type,
                tier
            );
            return false;
        }

        if (allowedItems.Contains(platformMetadata.Metadata.DisplayName))
        {
            logger.LogInformation(
                "Item {ItemName} of type {Type} is eligible for deployment to workspace {WorkspaceId}.",
                platformMetadata.Metadata.DisplayName,
                platformMetadata.Metadata.Type,
                workspaceId
            );
            return true;
        }

        logger.LogInformation(
            "Item {ItemName} of type {Type} is not eligible for deployment to workspace {WorkspaceId}.",
            platformMetadata.Metadata.DisplayName,
            platformMetadata.Metadata.Type,
            workspaceId
        );
        return false;
    }

    public static WorkspaceConfig? GetWorkspaceConfig(
        WorkspaceConfigList workspaceConfigs,
        Guid workspaceId,
        ILogger logger
    )
    {
        var workspaceConfig = workspaceConfigs.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
        if (workspaceConfig == null)
        {
            logger.LogWarning(
                "Workspace ID {WorkspaceId} not found in configurations.",
                workspaceId
            );
        }
        return workspaceConfig;
    }
}
