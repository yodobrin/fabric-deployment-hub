var builder = WebApplication.CreateBuilder(args);

// Log the current environment
var environment = builder.Environment.EnvironmentName;
Console.WriteLine($"Current Environment: {environment}");

// Retrieve configuration values
var configurationContainer = builder.Configuration["FABRIC_TENANT_CONFIG"];
var azureTenant = builder.Configuration["AZURE_TENANT_ID"];
var fabricClientId = builder.Configuration["FABRIC_API_CLIENT_ID"];
var fabricClientSecret = builder.Configuration["FABRIC_API_CLIENT_SECRET"];
var fabricCodeStoreUri = builder.Configuration["FABRIC_CODE_STORE_URI"];
var hubManagedIdentity = builder.Configuration["HUB_MANAGED_IDENTITY"];

if (string.IsNullOrEmpty(configurationContainer) || 
    string.IsNullOrEmpty(azureTenant) || 
    string.IsNullOrEmpty(fabricClientId) || 
    string.IsNullOrEmpty(fabricClientSecret) || 
    string.IsNullOrEmpty(fabricCodeStoreUri) ||
    string.IsNullOrEmpty(hubManagedIdentity))
{
    throw new Exception("Incomplete Configuration!! Deployment-Hub could not start.");
}

Console.WriteLine($"Configuration Loaded: \n Config Container: {configurationContainer}\n Tenant: {azureTenant}\n ClientId: {fabricClientId} \n CodeStoreUri: {fabricCodeStoreUri}");

// Define authority and scopes
string authority = $"https://login.microsoftonline.com/{azureTenant}";
string[] scopes = new[] { "https://api.fabric.microsoft.com/.default" };

// Register TokenService
builder.Services.AddSingleton<ITokenService>(new TokenService(fabricClientId, fabricClientSecret, authority, scopes));

// Register BlobServiceClient as a singleton
builder.Services.AddSingleton(_ =>
{
    try
    {
        var credentialOptions = new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = hubManagedIdentity
        };

        var blobServiceClient = new BlobServiceClient(
            new Uri(fabricCodeStoreUri),
            new DefaultAzureCredential(credentialOptions)
        );

        // Log successful creation
        Console.WriteLine($"Successfully created BlobServiceClient using Managed Identity: {hubManagedIdentity}");
        return blobServiceClient;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to create BlobServiceClient: {ex.Message}");
        throw;
    }
});

// Register FabricTenantStateService
builder.Services.AddSingleton<IFabricTenantStateService, FabricTenantStateService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<FabricTenantStateService>>();
    var blobServiceClient = provider.GetRequiredService<BlobServiceClient>();
    return new FabricTenantStateService( blobServiceClient, configurationContainer,logger);
});
builder.Services.AddScoped<DeploymentProcessor>();
// Register PlannerService
builder.Services.AddSingleton<IPlannerService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<PlannerService>>();
    var blobServiceClient = provider.GetRequiredService<BlobServiceClient>();
    var tenantStateService = provider.GetRequiredService<IFabricTenantStateService>();
    return new PlannerService(logger, blobServiceClient, tenantStateService);
    
});


// Register FabricRestService
builder.Services.AddHttpClient<IFabricRestService, FabricRestService>();

// Add controllers and middleware
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); 
});

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();