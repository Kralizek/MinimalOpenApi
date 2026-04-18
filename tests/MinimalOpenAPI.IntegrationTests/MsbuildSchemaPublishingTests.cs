using System.Diagnostics;

namespace MinimalOpenAPI.IntegrationTests;

[TestFixture]
public class MsbuildSchemaPublishingTests
{
    [Test]
    public void BuildAndPublish_AlwaysIncludeAllOpenApiItems_AndForwardSchemaMetadata()
    {
        var repoRoot = FindRepoRoot();
        var projectDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var targetsPath = Path.Combine(repoRoot, "src", "MinimalOpenAPI", "buildTransitive", "MinimalOpenAPI.targets")
                .Replace("\\", "/");

            File.WriteAllText(Path.Combine(projectDir, "Program.cs"), """
                using MinimalOpenAPI;

                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddMinimalOpenApi();
                var app = builder.Build();
                app.MapOpenApiSchemas();
                app.Run();
                """);

            File.WriteAllText(Path.Combine(projectDir, "openapi.yaml"), """
                openapi: "3.0.0"
                info:
                  title: Should Not Be Used
                  version: "9.9.9"
                paths: {}
                """);
            File.WriteAllText(Path.Combine(projectDir, "billing.json"), """
                {"openapi":"3.0.0","info":{"title":"Billing","version":"2.0.0"},"paths":{}}
                """);

            File.WriteAllText(Path.Combine(projectDir, "SchemaPublishingTest.csproj"), $"""
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
                    <CompilerGeneratedFilesOutputPath>obj/gen</CompilerGeneratedFilesOutputPath>
                  </PropertyGroup>

                  <Import Project="{targetsPath}" />

                  <ItemGroup>
                    <OpenApi Include="openapi.yaml"
                             PublishAs="/openapi/schema.yaml"
                             DisplayName="Todo API"
                             DisplayVersion="1.0.0" />
                    <OpenApi Include="billing.json" />
                  </ItemGroup>
                </Project>
                """);

            var build = RunDotNet(projectDir, "build SchemaPublishingTest.csproj -nologo");
            Assert.That(build.ExitCode, Is.EqualTo(0), build.Output);

            var buildSchemasDir = Path.Combine(projectDir, "bin", "Debug", "net10.0", "openapi", "schemas");
            Assert.That(Directory.Exists(buildSchemasDir), Is.True, "Build output must include openapi/schemas.");
            var builtFiles = Directory.EnumerateFiles(buildSchemasDir, "*.*", SearchOption.AllDirectories)
                .Select(Path.GetFileName)
                .ToArray();
            Assert.That(builtFiles, Contains.Item("openapi.yaml"));
            Assert.That(builtFiles, Contains.Item("billing.json"));

            var generatedDiFile = Directory.EnumerateFiles(
                Path.Combine(projectDir, "obj", "gen"),
                "*DependencyInjection.g.cs",
                SearchOption.AllDirectories).Single();
            var generatedSource = File.ReadAllText(generatedDiFile);
            Assert.That(generatedSource, Does.Contain("RegisterSchemaFile(\"openapi/schemas/"));
            Assert.That(generatedSource, Does.Contain("\", \"/openapi/schema.yaml\", \"Todo API\", \"1.0.0\")"));

            var publishDir = Path.Combine(projectDir, "published");
            var publish = RunDotNet(projectDir, $"publish SchemaPublishingTest.csproj -nologo -o \"{publishDir}\"");
            Assert.That(publish.ExitCode, Is.EqualTo(0), publish.Output);

            var publishedSchemasDir = Path.Combine(publishDir, "openapi", "schemas");
            Assert.That(Directory.Exists(publishedSchemasDir), Is.True, "Publish output must include openapi/schemas.");
            var publishedFiles = Directory.EnumerateFiles(publishedSchemasDir, "*.*", SearchOption.AllDirectories)
                .Select(Path.GetFileName)
                .ToArray();
            Assert.That(publishedFiles, Contains.Item("openapi.yaml"));
            Assert.That(publishedFiles, Contains.Item("billing.json"));
        }
        finally
        {
            Directory.Delete(projectDir, recursive: true);
        }
    }

    [Test]
    public void Build_WithDuplicatePublishAs_FailsValidation()
    {
        var repoRoot = FindRepoRoot();
        var projectDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var targetsPath = Path.Combine(repoRoot, "src", "MinimalOpenAPI", "buildTransitive", "MinimalOpenAPI.targets")
                .Replace("\\", "/");

            File.WriteAllText(Path.Combine(projectDir, "Program.cs"), """
                using MinimalOpenAPI;

                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddMinimalOpenApi();
                var app = builder.Build();
                app.MapOpenApiSchemas();
                app.Run();
                """);

            File.WriteAllText(Path.Combine(projectDir, "first.yaml"), """
                openapi: "3.0.0"
                paths: {}
                """);
            File.WriteAllText(Path.Combine(projectDir, "second.yaml"), """
                openapi: "3.0.0"
                paths: {}
                """);

            File.WriteAllText(Path.Combine(projectDir, "SchemaPublishingDuplicatePathTest.csproj"), $"""
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>

                  <Import Project="{targetsPath}" />

                  <ItemGroup>
                    <OpenApi Include="first.yaml" PublishAs="/openapi/schema.yaml" />
                    <OpenApi Include="second.yaml" PublishAs="/openapi/schema.yaml" />
                  </ItemGroup>
                </Project>
                """);

            var build = RunDotNet(projectDir, "build SchemaPublishingDuplicatePathTest.csproj -nologo");
            Assert.That(build.ExitCode, Is.Not.EqualTo(0), "Build must fail when PublishAs values are duplicated.");
            Assert.That(build.Output, Does.Contain("Duplicate PublishAs values found"));
        }
        finally
        {
            Directory.Delete(projectDir, recursive: true);
        }
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Directory.Build.props")) &&
                Directory.Exists(Path.Combine(current.FullName, "src", "MinimalOpenAPI")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static (int ExitCode, string Output) RunDotNet(string workingDirectory, string arguments)
    {
        var psi = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output + Environment.NewLine + error);
    }
}
