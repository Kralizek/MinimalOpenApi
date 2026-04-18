using System.Net.Http.Json;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Benchmark;

[MemoryDiagnoser]
[InProcess]
public class TodoApiBenchmarks
{
    private WebApplicationFactory<WithMinimalOpenApi.AppMarker> _withFactory = null!;
    private WebApplicationFactory<WithoutMinimalOpenApi.AppMarker> _withoutFactory = null!;
    private HttpClient _withClient = null!;
    private HttpClient _withoutClient = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _withFactory = new WithMinimalOpenApiFactory();
        _withoutFactory = new WithoutMinimalOpenApiFactory();

        _withClient = _withFactory.CreateClient();
        _withoutClient = _withoutFactory.CreateClient();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _withClient.Dispose();
        _withoutClient.Dispose();
        _withFactory.Dispose();
        _withoutFactory.Dispose();
    }

    [Benchmark]
    public Task CrudFlow_WithMinimalOpenApi() => RunCrudFlowAsync(_withClient);

    [Benchmark(Baseline = true)]
    public Task CrudFlow_WithoutMinimalOpenApi() => RunCrudFlowAsync(_withoutClient);

    [Benchmark]
    public Task Create_WithMinimalOpenApi() => RunCreateAsync(_withClient);

    [Benchmark]
    public Task Create_WithoutMinimalOpenApi() => RunCreateAsync(_withoutClient);

    [Benchmark]
    public Task Get_ById_WithMinimalOpenApi() => RunGetByIdAsync(_withClient);

    [Benchmark]
    public Task Get_ById_WithoutMinimalOpenApi() => RunGetByIdAsync(_withoutClient);

    [Benchmark]
    public Task List_Completed_WithMinimalOpenApi() => RunListCompletedAsync(_withClient);

    [Benchmark]
    public Task List_Completed_WithoutMinimalOpenApi() => RunListCompletedAsync(_withoutClient);

    [Benchmark]
    public Task Update_ById_WithMinimalOpenApi() => RunUpdateByIdAsync(_withClient);

    [Benchmark]
    public Task Update_ById_WithoutMinimalOpenApi() => RunUpdateByIdAsync(_withoutClient);

    [Benchmark]
    public Task Delete_ById_WithMinimalOpenApi() => RunDeleteByIdAsync(_withClient);

    [Benchmark]
    public Task Delete_ById_WithoutMinimalOpenApi() => RunDeleteByIdAsync(_withoutClient);

    private static async Task RunCrudFlowAsync(HttpClient client)
    {
        var createdTodo = await CreateTodoAndReadAsync(client, "benchmark-title", "benchmark-notes");

        using var getResponse = await client.GetAsync($"/todos/{createdTodo.Id}");
        getResponse.EnsureSuccessStatusCode();

        var updateRequest = new UpdateTodoRequest(
            Title: "benchmark-title-updated",
            Completed: true,
            DueDate: createdTodo.DueDate,
            Notes: "benchmark-notes-updated");

        using var updateResponse = await client.PutAsJsonAsync($"/todos/{createdTodo.Id}", updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        using var listResponse = await client.GetAsync("/todos?completed=true");
        listResponse.EnsureSuccessStatusCode();

        using var deleteResponse = await client.DeleteAsync($"/todos/{createdTodo.Id}");
        deleteResponse.EnsureSuccessStatusCode();
    }

    private static async Task RunCreateAsync(HttpClient client)
    {
        var createdTodo = await CreateTodoAndReadAsync(client, "create-op-title", "create-op-notes");

        // Keep state bounded across iterations.
        using var deleteResponse = await client.DeleteAsync($"/todos/{createdTodo.Id}");
        deleteResponse.EnsureSuccessStatusCode();
    }

    private static async Task RunGetByIdAsync(HttpClient client)
    {
        var createdTodo = await CreateTodoAndReadAsync(client, "get-op-title", "get-op-notes");

        using var getResponse = await client.GetAsync($"/todos/{createdTodo.Id}");
        getResponse.EnsureSuccessStatusCode();

        using var deleteResponse = await client.DeleteAsync($"/todos/{createdTodo.Id}");
        deleteResponse.EnsureSuccessStatusCode();
    }

    private static async Task RunListCompletedAsync(HttpClient client)
    {
        var createdTodo = await CreateTodoAndReadAsync(client, "list-op-title", "list-op-notes");

        var updateRequest = new UpdateTodoRequest(
            Title: "list-op-title-updated",
            Completed: true,
            DueDate: createdTodo.DueDate,
            Notes: "list-op-notes-updated");

        using var updateResponse = await client.PutAsJsonAsync($"/todos/{createdTodo.Id}", updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        using var listResponse = await client.GetAsync("/todos?completed=true");
        listResponse.EnsureSuccessStatusCode();

        using var deleteResponse = await client.DeleteAsync($"/todos/{createdTodo.Id}");
        deleteResponse.EnsureSuccessStatusCode();
    }

    private static async Task RunUpdateByIdAsync(HttpClient client)
    {
        var createdTodo = await CreateTodoAndReadAsync(client, "update-op-title", "update-op-notes");

        var updateRequest = new UpdateTodoRequest(
            Title: "update-op-title-updated",
            Completed: true,
            DueDate: createdTodo.DueDate,
            Notes: "update-op-notes-updated");

        using var updateResponse = await client.PutAsJsonAsync($"/todos/{createdTodo.Id}", updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        using var deleteResponse = await client.DeleteAsync($"/todos/{createdTodo.Id}");
        deleteResponse.EnsureSuccessStatusCode();
    }

    private static async Task RunDeleteByIdAsync(HttpClient client)
    {
        var createdTodo = await CreateTodoAndReadAsync(client, "delete-op-title", "delete-op-notes");

        using var deleteResponse = await client.DeleteAsync($"/todos/{createdTodo.Id}");
        deleteResponse.EnsureSuccessStatusCode();
    }

    private static async Task<TodoItem> CreateTodoAndReadAsync(HttpClient client, string title, string notes)
    {
        var createRequest = new CreateTodoRequest(
            Title: title,
            DueDate: DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)),
            Notes: notes);

        using var createResponse = await client.PostAsJsonAsync("/todos", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoItem>();
        if (createdTodo is null)
        {
            throw new InvalidOperationException("Create response body was empty.");
        }

        return createdTodo;
    }

    private sealed record CreateTodoRequest(string Title, DateOnly DueDate, string? Notes);
    private sealed record UpdateTodoRequest(string? Title, bool? Completed, DateOnly? DueDate, string? Notes);
    private sealed record TodoItem(Guid Id, string Title, bool Completed, DateTimeOffset CreatedAt, DateOnly? DueDate, string? Notes);

    private static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "MinimalOpenApi.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root containing MinimalOpenApi.slnx.");
    }

    private sealed class WithMinimalOpenApiFactory : WebApplicationFactory<WithMinimalOpenApi.AppMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var repoRoot = GetRepositoryRoot();
            builder.UseContentRoot(Path.Combine(repoRoot, "benchmarks", "WithMinimalOpenAPI"));
        }
    }

    private sealed class WithoutMinimalOpenApiFactory : WebApplicationFactory<WithoutMinimalOpenApi.AppMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var repoRoot = GetRepositoryRoot();
            builder.UseContentRoot(Path.Combine(repoRoot, "benchmarks", "WithoutMinimalOpenApi"));
        }
    }
}