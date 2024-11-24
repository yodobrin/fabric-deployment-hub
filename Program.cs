using FabricDeploymentHub;

var builder = WebApplication.CreateBuilder(args);

// Log the current environment
var environment = builder.Environment.EnvironmentName;
Console.WriteLine($"Current Environment: {environment}");

// Log the ConfigurationDirectory path
var configurationDirectory = builder.Configuration["ConfigurationDirectory"];

var azureTenant = builder.Configuration["AZURE_TENANT_ID"];
var fabricClientId = builder.Configuration["FABRIC_API_CLIENT_ID"];
var fabricClientSecret = builder.Configuration["FABRIC_API_CLIENT_SECRET"];
// temp containers would hold the entire code base from github
var fabricCodeStoreUri = builder.Configuration["FABRIC_CODE_STORE_URI"];

Console.WriteLine($"Configuration Loaded: \n Directory: {configurationDirectory}\n Tenant: {azureTenant}\n ClientId: {fabricClientId} \n CodeStoreUri: {fabricCodeStoreUri}");

if (string.IsNullOrEmpty(configurationDirectory) || string.IsNullOrEmpty(azureTenant) || string.IsNullOrEmpty(fabricClientId) || string.IsNullOrEmpty(fabricClientSecret)|| string.IsNullOrEmpty(fabricCodeStoreUri))
{
    throw new Exception("Incomplete Configuration!! Deployment-Hub could not start.");
}

string authority = $"https://login.microsoftonline.com/{azureTenant}";
string[] scopes = new[] { "https://fabric.microsoft.com/.default" };

// Register TokenService
builder.Services.AddSingleton<ITokenService>(new TokenService(fabricClientId, fabricClientSecret, authority, scopes));
// Register the fabricCodeStoreUri as a singleton
builder.Services.AddSingleton(fabricCodeStoreUri);

// Register PlannerService and pass the configuration directory as a parameter
builder.Services.AddSingleton<IPlannerService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<PlannerService>>();    
    return new PlannerService(logger, configurationDirectory);
});

// Register WorkspaceStateService with DI container
builder.Services.AddSingleton<IWorkspaceStateService, WorkspaceStateService>();
builder.Services.AddHttpClient<IFabricRestService, FabricRestService>();
// Add other services and middleware as needed
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();