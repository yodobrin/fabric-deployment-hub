namespace FabricDeploymentHub.Services;

public interface ITokenService
{
    Task<string> GetAccessTokenAsync();
}
