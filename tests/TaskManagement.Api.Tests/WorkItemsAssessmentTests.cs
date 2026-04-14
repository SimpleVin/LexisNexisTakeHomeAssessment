using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskManagement.Api.Tests;

public class WorkItemsAssessmentTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _userClient = null!;
    private HttpClient _adminClient = null!;

    public WorkItemsAssessmentTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b => b.UseSetting(WebHostDefaults.EnvironmentKey, "Development"));
    }

    public async Task InitializeAsync()
    {
        var tokenClient = _factory.CreateClient();
        var userToken = await DevAuth.RequestBearerTokenAsync(tokenClient, "User", TestSeedMemberIds.VinUser);
        _userClient = _factory.CreateClient();
        _userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        var adminToken = await DevAuth.RequestBearerTokenAsync(tokenClient, "Admin", TestSeedMemberIds.VinAdmin);
        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task View_tasks_list_returns_seeded_rows()
    {
        var response = await _userClient.GetAsync("/api/work-items");
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<JsonEnvelope<PagedWorkItemsResponse>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        var page = envelope.Data;
        Assert.NotNull(page?.Items);
        Assert.True(page.TotalCount >= 3, "Seed should include at least three work items.");
        Assert.True(page.Items.Length >= 3);
        Assert.Equal(1, page.Page);
        Assert.True(page.PageSize >= 3);
    }

    [Fact]
    public async Task List_tasks_supports_pagination()
    {
        var response = await _userClient.GetAsync("/api/work-items?page=1&pageSize=2");
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<JsonEnvelope<PagedWorkItemsResponse>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        var page = envelope.Data;
        Assert.NotNull(page);
        Assert.Equal(2, page.Items.Length);
        Assert.True(page.TotalCount >= 3);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public async Task Search_tasks_by_title_q_parameter()
    {
        var response = await _userClient.GetAsync("/api/work-items?q=draft");
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<JsonEnvelope<PagedWorkItemsResponse>>(JsonOptions);
        Assert.NotNull(envelope?.Data?.Items);
        Assert.Contains(envelope.Data.Items, x => x.Title.Contains("Draft", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Filter_tasks_by_status()
    {
        var response = await _userClient.GetAsync("/api/work-items?status=InProgress");
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<JsonEnvelope<PagedWorkItemsResponse>>(JsonOptions);
        Assert.NotNull(envelope?.Data?.Items);
        Assert.All(envelope.Data.Items, x => Assert.Equal("InProgress", x.Status));
    }

    [Fact]
    public async Task Filter_tasks_by_assigneeId()
    {
        var assigneeId = TestSeedMemberIds.JonoMulaudzi.ToString();
        var response = await _userClient.GetAsync($"/api/work-items?assigneeId={assigneeId}");
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<JsonEnvelope<PagedWorkItemsResponse>>(JsonOptions);
        Assert.NotNull(envelope?.Data?.Items);
        Assert.All(envelope.Data.Items, x => Assert.Equal(assigneeId, x.AssigneeId?.ToString()));
    }

    [Fact]
    public async Task Filter_tasks_by_priority()
    {
        var response = await _userClient.GetAsync("/api/work-items?priority=High");
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<JsonEnvelope<PagedWorkItemsResponse>>(JsonOptions);
        Assert.NotNull(envelope?.Data?.Items);
        Assert.All(envelope.Data.Items, x => Assert.Equal("High", x.Priority));
    }

    [Fact]
    public async Task Create_task_without_status_defaults_to_New()
    {
        var memberId = TestSeedMemberIds.SamBaloyi.ToString();
        var create = await _userClient.PostAsJsonAsync(
            "/api/work-items",
            new
            {
                title = "No status in body",
                description = (string?)null,
                priority = "Medium",
                assigneeId = memberId,
            });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var envelope = await create.Content.ReadFromJsonAsync<JsonEnvelope<WorkItemResponse>>(JsonOptions);
        Assert.Equal("New", envelope?.Data?.Status);
    }

    [Fact]
    public async Task Create_update_delete_task_and_assign_member_and_priority()
    {
        var memberId = TestSeedMemberIds.SamBaloyi.ToString();
        var assignerId = TestSeedMemberIds.VinUser;

        var create = await _userClient.PostAsJsonAsync(
            "/api/work-items",
            new
            {
                title = "Assessment integration task",
                description = (string?)null,
                status = (string?)"Todo",
                priority = "Medium",
                assigneeId = memberId,
            });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var createdEnvelope = await create.Content.ReadFromJsonAsync<JsonEnvelope<WorkItemResponse>>(JsonOptions);
        Assert.NotNull(createdEnvelope?.Data?.Id);
        Assert.Equal("Medium", createdEnvelope.Data.Priority);
        Assert.Equal(memberId, createdEnvelope.Data.AssigneeId?.ToString());
        Assert.Equal(assignerId, createdEnvelope.Data.AssignerId);

        var get = await _userClient.GetAsync($"/api/work-items/{createdEnvelope.Data.Id}");
        get.EnsureSuccessStatusCode();

        var update = await _userClient.PutAsJsonAsync(
            $"/api/work-items/{createdEnvelope.Data.Id}",
            new
            {
                title = "Assessment integration task",
                description = (string?)null,
                status = "InProgress",
                priority = "High",
                assigneeId = memberId,
            });
        update.EnsureSuccessStatusCode();
        var updatedEnvelope = await update.Content.ReadFromJsonAsync<JsonEnvelope<WorkItemResponse>>(JsonOptions);
        Assert.Equal("High", updatedEnvelope?.Data?.Priority);
        Assert.Equal("InProgress", updatedEnvelope?.Data?.Status);
        Assert.Equal(assignerId, updatedEnvelope?.Data?.AssignerId);

        var delete = await _adminClient.DeleteAsync($"/api/work-items/{createdEnvelope.Data.Id}");
        delete.EnsureSuccessStatusCode();
        var deleteEnvelope = await delete.Content.ReadFromJsonAsync<JsonUnitEnvelope>(JsonOptions);
        Assert.NotNull(deleteEnvelope);
        Assert.True(deleteEnvelope.Success);
        Assert.False(string.IsNullOrWhiteSpace(deleteEnvelope.Message));

        var gone = await _userClient.GetAsync($"/api/work-items/{createdEnvelope.Data.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    private sealed record JsonEnvelope<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
    }

    private sealed record JsonUnitEnvelope
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
    }

    private sealed record PagedWorkItemsResponse
    {
        public WorkItemResponse[] Items { get; init; } = [];
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
        public bool HasPreviousPage { get; init; }
        public bool HasNextPage { get; init; }
    }

    private sealed record WorkItemResponse
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = "";
        public string? Description { get; init; }
        public string? Status { get; init; }
        public string Priority { get; init; } = "";
        public Guid? AssigneeId { get; init; }
        public Guid? AssignerId { get; init; }
    }
}
