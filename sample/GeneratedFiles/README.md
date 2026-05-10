# GeneratedFiles

This sample demonstrates how to inspect generated source files emitted by MinimalOpenAPI (and other Roslyn source generators).

## What it demonstrates

- `EmitCompilerGeneratedFiles=true` — tells Roslyn to write generated source files to disk
- `CompilerGeneratedFilesOutputPath=Generated` — controls where the files land
- `Compile Remove="Generated/**/*.cs"` — prevents duplicate compilation when the output folder is inside the project tree
- Where MinimalOpenAPI generated files appear in the output tree
- What generated files look like (DTO records, endpoint base classes, DI wiring)

## Important project file settings

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>

<ItemGroup>
  <Compile Remove="Generated/**/*.cs" />
</ItemGroup>
```

The `Compile Remove` is **required** when the output folder is inside the project tree. Without it, the compiler would compile the emitted files a second time, causing duplicate type definition errors.

## Finding MinimalOpenAPI generated files

After `dotnet build`, look under:

```
Generated/
  MinimalOpenApi/               ← MinimalOpenAPI generator subtree
    Openapi/                    ← derived from the spec filename (openapi.yaml → Openapi)
      Schemas/
        GetWidgetEndpointBase+OkResponse.cs   ← generated inline DTO record
      Operations/
        GetWidgetEndpointBase.cs              ← generated abstract base class
      Infrastructure/
        ...                                   ← DI registration and endpoint mapping
  Microsoft.AspNetCore.Http.RequestDelegateGenerator/ ← from another generator
  ...
```

## Note on generator scope

`EmitCompilerGeneratedFiles` emits files from **all** source generators used by the project — not only MinimalOpenAPI. For example, ASP.NET Core's request delegate generator and `System.Text.Json` source generators also emit files here. MinimalOpenAPI files are grouped under the `MinimalOpenApi/` subtree.

## Checking generated files into source control

Checking generated files into source control is optional. Teams can either:

- **Ignore the `Generated/` directory** (add `Generated/` to `.gitignore`) — simpler setup, requires building to see generated code.
- **Commit only the MinimalOpenAPI subtree** — makes generated code visible in the repo without committing other generator output.

## How to run

```shell
cd sample/GeneratedFiles
dotnet build   # generates files in Generated/
```

Then inspect:

```shell
find Generated/MinimalOpenApi -name "*.cs" | sort
```

To run the app:

```shell
dotnet run
curl http://localhost:5000/widgets/00000000-0000-0000-0000-000000000001
```
