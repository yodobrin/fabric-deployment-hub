using System.Net;
using System.Net.Http.Headers;

namespace FabricDeploymentHub.Services;

public class FabricRestService : IFabricRestService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenService _tokenService;
    private readonly ILogger<FabricRestService> _logger;
    private const string _fabricApiBaseUri = "https://api.fabric.microsoft.com/v1/";

    public FabricRestService(
        HttpClient httpClient,
        ITokenService tokenService,
        ILogger<FabricRestService> logger
    )
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<List<FabricItem>> GetWorkspaceItemsAsync(Guid workspaceId)
    {
        string accessToken = await _tokenService.GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );
        var response = await _httpClient.GetAsync(
            $"{_fabricApiBaseUri}workspaces/{workspaceId}/items"
        );

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to fetch items for workspace {workspaceId}. Status code: {response.StatusCode}"
            );
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var fabricItems = JsonSerializer.Deserialize<FabricItemsResponse>(
            jsonResponse,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        return fabricItems?.Value ?? new List<FabricItem>();
    }

    public async Task<string> GetAsync(string uri)
    {
        try
        {
            _logger.LogInformation("Initiating GET request to {Uri}", uri);

            string accessToken = await _tokenService.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken
            );
            var compoundUri = new Uri(new Uri(_fabricApiBaseUri), uri);

            var response = await _httpClient.GetAsync(compoundUri);

            LogResponse(response, "GET", uri);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GET request to {Uri}", uri);
            throw;
        }
    }

    public async Task<string> PostAsync(
        string uri,
        object payload,
        bool waitForCompletion = false,
        TimeSpan? pollingInterval = null,
        TimeSpan? timeout = null
    )
    {
        return await ExecuteWithCompletionHandling(
            HttpMethod.Post,
            uri,
            payload,
            waitForCompletion,
            pollingInterval,
            timeout
        );
    }

    public async Task<string> PatchAsync(
        string uri,
        object payload,
        bool waitForCompletion = false,
        TimeSpan? pollingInterval = null,
        TimeSpan? timeout = null
    )
    {
        return await ExecuteWithCompletionHandling(
            HttpMethod.Patch,
            uri,
            payload,
            waitForCompletion,
            pollingInterval,
            timeout
        );
    }

    public async Task DeleteAsync(
        string uri,
        bool waitForCompletion = false,
        TimeSpan? pollingInterval = null,
        TimeSpan? timeout = null
    )
    {
        await ExecuteWithCompletionHandling(
            HttpMethod.Delete,
            uri,
            null,
            waitForCompletion,
            pollingInterval,
            timeout
        );
    }

    private async Task<string> ExecuteWithCompletionHandling(
        HttpMethod method,
        string uri,
        object? payload,
        bool waitForCompletion,
        TimeSpan? pollingInterval,
        TimeSpan? timeout
    )
    {
        if (payload == null)
            throw new InvalidOperationException("Unexpected empty payload.");

        string accessToken = await _tokenService.GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );
        var compoundUri = new Uri(new Uri(_fabricApiBaseUri), uri);
        HttpRequestMessage request =
            new(method, compoundUri)
            {
                Content =
                    payload != null
                        ? new StringContent(
                            JsonSerializer.Serialize(
                                payload,
                                new JsonSerializerOptions { WriteIndented = true }
                            ),
                            Encoding.UTF8,
                            "application/json"
                        )
                        : null
            };

        try
        {
            // Sanitize payload for logging
            var sanitizedPayload = PayloadSanitizer.Sanitize(payload!, new[] { "payload" });
            _logger.LogInformation(
                "Sending {Method} request to {Uri} with payload: {Payload}",
                method,
                uri,
                JsonSerializer.Serialize(
                    sanitizedPayload,
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "{Method} request to {Uri} failed with status {StatusCode}. Response: {Response}",
                    method,
                    uri,
                    response.StatusCode,
                    responseContent
                );
            }
            response.EnsureSuccessStatusCode();

            if (waitForCompletion && response.StatusCode == HttpStatusCode.Accepted)
            {
                var location =
                    response.Headers.Location?.ToString()
                    ?? throw new InvalidOperationException("Location header missing in response.");
                return await WaitForCompletionAsync(
                    location,
                    pollingInterval ?? TimeSpan.FromSeconds(5),
                    timeout ?? TimeSpan.FromSeconds(25)
                );
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during {Method} request to {Uri}.", method, uri);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during {Method} request to {Uri}.", method, uri);
            throw;
        }
    }

    private async Task<string> WaitForCompletionAsync(
        string location,
        TimeSpan pollingInterval,
        TimeSpan timeout
    )
    {
        try
        {
            _logger.LogInformation("Polling for completion at {Location}", location);

            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime < timeout)
            {
                string accessToken = await _tokenService.GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken
                );

                var response = await _httpClient.GetAsync(location);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Operation at {Location} completed successfully.",
                        location
                    );
                    return await response.Content.ReadAsStringAsync();
                }

                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    _logger.LogWarning(
                        "Unexpected status code {StatusCode} during polling for {Location}.",
                        response.StatusCode,
                        location
                    );
                    throw new HttpRequestException(
                        $"Unexpected status code: {response.StatusCode}"
                    );
                }

                await Task.Delay(pollingInterval);
            }

            throw new TimeoutException(
                $"Operation did not complete within the specified timeout of {timeout.TotalSeconds} seconds."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during polling for completion at {Location}.", location);
            throw;
        }
    }

    private async Task<string> HandleNonWaitingScenario(HttpResponseMessage response)
    {
        if (response.Headers.Location != null)
        {
            _logger.LogInformation(
                "Response includes location header: {Location}",
                response.Headers.Location
            );
            return response.Headers.Location.ToString();
        }
        else if (
            response.StatusCode == HttpStatusCode.OK
            || response.StatusCode == HttpStatusCode.NoContent
        )
        {
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            _logger.LogWarning(
                "Unexpected response status code: {StatusCode}",
                response.StatusCode
            );
            throw new InvalidOperationException("Unexpected response without Location header.");
        }
    }

    private void LogResponse(HttpResponseMessage response, string method, string uri)
    {
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "{Method} request to {Uri} succeeded with status code {StatusCode}.",
                method,
                uri,
                response.StatusCode
            );
        }
        else
        {
            _logger.LogWarning(
                "{Method} request to {Uri} failed with status code {StatusCode}.",
                method,
                uri,
                response.StatusCode
            );
        }
    }
}
