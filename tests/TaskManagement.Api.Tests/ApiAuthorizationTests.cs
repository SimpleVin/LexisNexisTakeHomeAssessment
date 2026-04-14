using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskManagement.Api.Tests;

public class ApiAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b => b.UseSetting(WebHostDefaults.EnvironmentKey, "Development"));
    }

    [Fact]
    public async Task Health_does_not_require_authentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Work_items_require_authentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/work-items");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>(jsonOptions);
        Assert.Equal(401, problem?.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem?.Detail));
    }

    [Fact]
    public async Task User_role_cannot_delete_work_items()
    {
        var client = _factory.CreateClient();
        var token = await DevAuth.RequestBearerTokenAsync(client, "User", TestSeedMemberIds.VinUser);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync($"/api/work-items/{TestSeedWorkItemIds.DraftApiSpecification}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>(jsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(problem?.Detail));
        Assert.Contains("permission", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task User_role_cannot_delete_team_members_and_gets_problem_details()
    {
        var client = _factory.CreateClient();
        var token = await DevAuth.RequestBearerTokenAsync(client, "User", TestSeedMemberIds.VinUser);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync($"/api/team-members/{TestSeedMemberIds.SamBaloyi}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>(jsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(problem?.Detail));
        Assert.Contains("permission", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_role_can_delete_work_items()
    {
        var client = _factory.CreateClient();
        var token = await DevAuth.RequestBearerTokenAsync(client, "Admin", TestSeedMemberIds.VinAdmin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync(
            "/api/work-items",
            new
            {
                title = "Auth admin delete test",
                description = (string?)null,
                status = (string?)"Todo",
                priority = "Low",
                assigneeId = (string?)null,
            });
        create.EnsureSuccessStatusCode();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var envelope = await create.Content.ReadFromJsonAsync<JsonEnvelope>(jsonOptions);
        Assert.NotNull(envelope?.Data?.Id);

        var delete = await client.DeleteAsync($"/api/work-items/{envelope.Data.Id}");
        delete.EnsureSuccessStatusCode();
        var deleteBody = await delete.Content.ReadFromJsonAsync<DeleteUnitEnvelope>(jsonOptions);
        Assert.True(deleteBody?.Success);
    }

    private sealed record JsonEnvelope
    {
        public bool Success { get; init; }
        public WorkItemIdPayload? Data { get; init; }
    }

    private sealed record ProblemDetailsPayload
    {
        public string? Title { get; init; }
        public int Status { get; init; }
        public string? Detail { get; init; }
    }

    private sealed record WorkItemIdPayload
    {
        public Guid Id { get; init; }
    }

    private sealed record DeleteUnitEnvelope
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
    }
}
