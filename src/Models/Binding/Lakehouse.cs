namespace FabricDeploymentHub.Models.Binding;

public class LakehouseResponse
{
    [JsonPropertyName("value")]
    public List<LakehouseDetails> Value { get; set; } = new();
}

public class LakehouseDetails
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public LakehouseProperties Properties { get; set; } = new();

    [JsonPropertyName("folderId")]
    public string FolderId { get; set; } = string.Empty;
}

public class LakehouseProperties
{
    [JsonPropertyName("oneLakeTablesPath")]
    public string OneLakeTablesPath { get; set; } = string.Empty;

    [JsonPropertyName("oneLakeFilesPath")]
    public string OneLakeFilesPath { get; set; } = string.Empty;

    [JsonPropertyName("sqlEndpointProperties")]
    public SqlEndpointProperties SqlEndpointProperties { get; set; } = new();
}

public class SqlEndpointProperties
{
    [JsonPropertyName("connectionString")]
    public string ConnectionString { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("provisioningStatus")]
    public string ProvisioningStatus { get; set; } = string.Empty;
}
