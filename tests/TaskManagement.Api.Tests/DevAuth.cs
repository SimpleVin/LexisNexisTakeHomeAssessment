using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagement.Api.Tests;

internal static class DevAuth
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    internal static async Task<string> RequestBearerTokenAsync(HttpClient client, string role, Guid teamMemberId)
    {
        var response = await client.PostAsJsonAsync("/api/dev/token", new { role, teamMemberId });
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DevTokenResponse>(JsonOptions);
        if (string.IsNullOrEmpty(dto?.AccessToken))
        {
            throw new InvalidOperationException("Dev token response did not include accessToken.");
        }

        return dto.AccessToken;
    }

    private sealed record DevTokenResponse
    {
        public string AccessToken { get; init; } = "";
    }
}
