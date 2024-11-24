using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FabricDeploymentHub.Services;

public class FabricRestService : IFabricRestService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenService _tokenService;

    public FabricRestService(HttpClient httpClient, ITokenService tokenService)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
    }

    public async Task<string> GetAsync(string uri)
    {
        string accessToken = await _tokenService.GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> PostAsync(string uri, object payload, bool waitForCompletion = false, TimeSpan? pollingInterval = null, TimeSpan? timeout = null)
    {
        return await ExecuteWithCompletionHandling(HttpMethod.Post, uri, payload, waitForCompletion, pollingInterval, timeout);
    }

    public async Task<string> PatchAsync(string uri, object payload, bool waitForCompletion = false, TimeSpan? pollingInterval = null, TimeSpan? timeout = null)
    {
        return await ExecuteWithCompletionHandling(HttpMethod.Patch, uri, payload, waitForCompletion, pollingInterval, timeout);
    }

    public async Task DeleteAsync(string uri, bool waitForCompletion = false, TimeSpan? pollingInterval = null, TimeSpan? timeout = null)
    {
        await ExecuteWithCompletionHandling(HttpMethod.Delete, uri, null, waitForCompletion, pollingInterval, timeout);
    }

    private async Task<string> ExecuteWithCompletionHandling(HttpMethod method, string uri, object? payload, bool waitForCompletion, TimeSpan? pollingInterval, TimeSpan? timeout)
    {
        string accessToken = await _tokenService.GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpRequestMessage request = new(method, uri)
        {
            Content = payload != null
                ? new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                : null
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        if (waitForCompletion)
        {
            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                // Poll for completion if 202 Accepted 
                var location = response.Headers.Location?.ToString() ?? throw new InvalidOperationException("Location header missing in response.");
                return await WaitForCompletionAsync(location, pollingInterval ?? TimeSpan.FromSeconds(5), timeout ?? TimeSpan.FromSeconds(25));
            }
            else
            {
                // For 200 OK or other success codes, return the response content
                return await response.Content.ReadAsStringAsync();
            }
        }

        // Handle non-waiting scenarios
        if (response.Headers.Location != null)
        {
            return response.Headers.Location.ToString();
        }
        else if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
        {
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            throw new InvalidOperationException("Unexpected response without Location header.");
        }
    }

    private async Task<string> WaitForCompletionAsync(string location, TimeSpan pollingInterval, TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            string accessToken = await _tokenService.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync(location);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                throw new HttpRequestException($"Unexpected status code: {response.StatusCode}");
            }

            await Task.Delay(pollingInterval);
        }

        throw new TimeoutException($"Operation did not complete within the specified timeout of {timeout.TotalMinutes} minutes.");
    }
}