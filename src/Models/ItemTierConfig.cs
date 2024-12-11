namespace FabricDeploymentHub.Models;

public class ItemTierConfig
{
    public Dictionary<string, TierConfig> Tiers { get; set; } =
        new Dictionary<string, TierConfig>();
}

public class TierConfig
{
    public Dictionary<string, List<string>> Items { get; set; } =
        new Dictionary<string, List<string>>();
}
