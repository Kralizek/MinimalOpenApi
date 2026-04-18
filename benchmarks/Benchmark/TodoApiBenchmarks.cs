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

    private static async Task RunCrudFlowAsync(HttpClient client)
    {
        var createRequest = new CreateTodoRequest(
            Title: "benchmark-title",
            DueDate: DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)),
            Notes: "benchmark-notes");

        using var createResponse = await client.PostAsJsonAsync("/todos", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoItem>();
        if (createdTodo is null)
        {
            throw new InvalidOperationException("Create response body was empty.");
        }

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