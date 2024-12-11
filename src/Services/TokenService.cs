namespace FabricDeploymentHub.Services;

public class TokenService : ITokenService
{
    private readonly IConfidentialClientApplication _app;
    private string _accessToken = string.Empty;
    private DateTimeOffset _accessTokenExpiresOn = DateTimeOffset.MinValue;

    public TokenService(string clientId, string clientSecret, string authority, string[] scopes)
    {
        _app = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(authority)
            .Build();

        Scopes = scopes;
    }

    private string[] Scopes { get; }

    public async Task<string> GetAccessTokenAsync()
    {
        // Check if token is still valid
        if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _accessTokenExpiresOn)
        {
            // Token is still valid
            return _accessToken;
        }

        // Token is expired or not acquired yet
        try
        {
            AuthenticationResult result = await _app.AcquireTokenForClient(Scopes).ExecuteAsync();

            // Update the token and its expiration time
            _accessToken = result.AccessToken;
            _accessTokenExpiresOn = result.ExpiresOn;

            return _accessToken;
        }
        catch (MsalServiceException ex)
        {
            Console.WriteLine($"MsalServiceException when acquiring token: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception when acquiring token: {ex.Message}");
            throw;
        }
    }
}
