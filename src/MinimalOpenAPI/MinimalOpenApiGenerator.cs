using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using MinimalOpenAPI.Abstractions;
using MinimalOpenAPI.Abstractions.Models;
using MinimalOpenAPI.Generator.CodeGen;
using MinimalOpenAPI.Generator.Diagnostics;
using MinimalOpenAPI.Parser.Json;
using MinimalOpenAPI.Parser.Yaml;

namespace MinimalOpenAPI.Generator;

/// <summary>
/// Roslyn incremental source generator that reads OpenAPI specification files and emits
/// strongly-typed DTOs, abstract handler base classes, registration customizer bases,
/// DI registration extensions and endpoint mapping extensions for use with ASP.NET Core
/// Minimal APIs.
/// </summary>
[Generator]
public sealed class MinimalOpenApiGenerator : IIncrementalGenerator
{
    private const string MetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiFile";
    private const string NamespaceMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiNamespace";
    private const string SchemaIdMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiSchemaId";
    private const string PublishAsMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiPublishAs";
    private const string DisplayNameMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiDisplayName";
    private const string DisplayVersionMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiDisplayVersion";
    private const string ReadWriteSchemaHandlingMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiReadWriteSchemaHandling";
    private const string RootNamespaceKey = "build_property.RootNamespace";

    /// <summary>
    /// Registers the incremental generation pipeline with Roslyn.
    /// This is called once by the compiler host; do not call it directly.
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Collect OpenAPI additional files
        var openApiFiles = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Where(pair =>
            {
                pair.Right.GetOptions(pair.Left).TryGetValue(MetadataKey, out var flag);
                return string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
            })
            .Select((pair, ct) =>
            {
                var options = pair.Right.GetOptions(pair.Left);
                var content = pair.Left.GetText(ct)?.ToString() ?? string.Empty;
                return CreateOpenApiFileInput(content, pair.Left.Path, options);
            });

        // 2. Get root namespace
        var rootNamespace = context.AnalyzerConfigOptionsProvider
            .Select((provider, _) =>
            {
                provider.GlobalOptions.TryGetValue(RootNamespaceKey, out var ns);
                return ns ?? "MinimalOpenAPI.Generated";
            });

        // 3. Parse OpenAPI documents — parser is selected via CanParse (format + optional version check)
        var parsedDocuments = openApiFiles
            .Combine(rootNamespace)
            .Select((pair, _) => ParseOpenApiFile(pair.Left, pair.Right));

        var duplicateSpecNames = openApiFiles
            .Collect()
            .Select((files, _) => CreateDuplicateSpecNameMap(files));

        // 4. Discover concrete class declarations in user code
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is ClassDeclarationSyntax cls &&
                    cls.BaseList?.Types.Count > 0 &&
                    !cls.Modifiers.Any(m => m.ValueText == "abstract"),
                transform: static (ctx, ct) =>
                {
                    var cls = (ClassDeclarationSyntax)ctx.Node;
                    var symbol = ctx.SemanticModel.GetDeclaredSymbol(cls, ct) as INamedTypeSymbol;
                    if (symbol is null || symbol.IsAbstract) return null;

                    var baseTypes = new List<string>();
                    var baseType = symbol.BaseType;
                    while (baseType is not null)
                    {
                        baseTypes.Add(baseType.Name);
                        baseType = baseType.BaseType;
                    }

                    return new ClassInfo(
                        FullName: symbol.ToDisplayString(),
                        Name: symbol.Name,
                        BaseTypeNames: baseTypes);
                })
            .Where(c => c is not null)
            .Select((c, _) => c!);

        // 5. Combine parsed docs with discovered classes
        var combined = parsedDocuments
            .Combine(classDeclarations.Collect())
            .Combine(duplicateSpecNames)
            .Select((pair, _) =>
                new GeneratorPipelineInput(
                    ParsedFile: pair.Left.Left,
                    Classes: pair.Left.Right,
                    DuplicatesBySpecName: pair.Right));

        // 6. Generate source files
        context.RegisterSourceOutput(combined, GenerateSource);
    }

    private static void GenerateSource(SourceProductionContext spc, GeneratorPipelineInput input)
    {
        if (!TryCreateGenerationInput(spc, input, out var generationInput))
            return;

        ReportUnknownOpenApiVersionIfNeeded(spc, generationInput);

        GenerateForDocument(
            spc,
            generationInput,
            input.Classes.ToArray());
    }

    private static bool TryCreateGenerationInput(
        SourceProductionContext spc,
        GeneratorPipelineInput input,
        out OpenApiGenerationInput generationInput)
    {
        var parsed = input.ParsedFile;

        if (input.DuplicatesBySpecName.TryGetValue(parsed.SpecName, out var conflictingFiles))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DuplicateSpecName,
                CreateOpenApiLocation(parsed.Path),
                parsed.SpecName,
                string.Join(", ", conflictingFiles)));
            generationInput = default!;
            return false;
        }

        if (parsed.UnsupportedExtension is not null)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UnsupportedFileExtension,
                CreateOpenApiLocation(parsed.Path),
                parsed.UnsupportedExtension, parsed.Path));
            generationInput = default!;
            return false;
        }

        if (parsed.ParseError is not null)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ParseError,
                CreateOpenApiLocation(parsed.Path),
                parsed.Path, parsed.ParseError));
            generationInput = default!;
            return false;
        }

        if (!parsed.CanGenerate)
        {
            generationInput = default!;
            return false;
        }

        if (!TryParseReadWriteSchemaHandling(parsed.ReadWriteSchemaHandling, out var readWriteSchemaHandling))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidReadWriteSchemaHandling,
                CreateOpenApiLocation(parsed.Path),
                parsed.Path,
                parsed.ReadWriteSchemaHandling ?? string.Empty));
            generationInput = default!;
            return false;
        }

        generationInput = parsed.ToGenerationInput(readWriteSchemaHandling);
        return true;
    }

    private static void ReportUnknownOpenApiVersionIfNeeded(
        SourceProductionContext spc,
        OpenApiGenerationInput input)
    {

        // Warn when the OpenAPI version is absent or not yet explicitly supported.
        if (!IsKnownVersion(input.Document.OpenApiVersion))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UnknownOpenApiVersion,
                CreateOpenApiLocation(input.Path),
                input.Path));
        }
    }

    private static OpenApiFileInput CreateOpenApiFileInput(
        string content,
        string path,
        AnalyzerConfigOptions options)
    {
        options.TryGetValue(NamespaceMetadataKey, out var explicitNamespace);
        options.TryGetValue(SchemaIdMetadataKey, out var schemaId);
        options.TryGetValue(PublishAsMetadataKey, out var publishAs);
        options.TryGetValue(DisplayNameMetadataKey, out var displayName);
        options.TryGetValue(DisplayVersionMetadataKey, out var displayVersion);
        options.TryGetValue(ReadWriteSchemaHandlingMetadataKey, out var readWriteSchemaHandling);
        var specName = DeriveSpecName(path, explicitNamespace);

        return new OpenApiFileInput(
            Content: content,
            Path: path,
            SpecName: specName,
            SchemaId: schemaId ?? string.Empty,
            PublishAs: string.IsNullOrWhiteSpace(publishAs) ? null : publishAs,
            DisplayName: string.IsNullOrWhiteSpace(displayName) ? null : displayName,
            DisplayVersion: string.IsNullOrWhiteSpace(displayVersion) ? null : displayVersion,
            ReadWriteSchemaHandling: string.IsNullOrWhiteSpace(readWriteSchemaHandling) ? null : readWriteSchemaHandling);
    }

    private static ParsedOpenApiFile ParseOpenApiFile(OpenApiFileInput file, string rootNamespace)
    {
        var parser = SelectParser(file.Path, file.Content);
        if (parser is null)
        {
            var ext = System.IO.Path.GetExtension(file.Path);
            return new ParsedOpenApiFile(
                Document: null,
                Path: file.Path,
                RootNamespace: rootNamespace,
                SpecName: file.SpecName,
                SchemaId: file.SchemaId,
                PublishAs: file.PublishAs,
                DisplayName: file.DisplayName,
                DisplayVersion: file.DisplayVersion,
                ReadWriteSchemaHandling: file.ReadWriteSchemaHandling,
                ParseError: null,
                UnsupportedExtension: ext);
        }

        try
        {
            var doc = parser.ParseAsync(file.Content).GetAwaiter().GetResult();
            return new ParsedOpenApiFile(
                Document: doc,
                Path: file.Path,
                RootNamespace: rootNamespace,
                SpecName: file.SpecName,
                SchemaId: file.SchemaId,
                PublishAs: file.PublishAs,
                DisplayName: file.DisplayName,
                DisplayVersion: file.DisplayVersion,
                ReadWriteSchemaHandling: file.ReadWriteSchemaHandling,
                ParseError: null,
                UnsupportedExtension: null);
        }
        catch (Exception ex)
        {
            return new ParsedOpenApiFile(
                Document: null,
                Path: file.Path,
                RootNamespace: rootNamespace,
                SpecName: file.SpecName,
                SchemaId: file.SchemaId,
                PublishAs: file.PublishAs,
                DisplayName: file.DisplayName,
                DisplayVersion: file.DisplayVersion,
                ReadWriteSchemaHandling: file.ReadWriteSchemaHandling,
                ParseError: ex.Message,
                UnsupportedExtension: null);
        }
    }

    private static IReadOnlyDictionary<string, string[]> CreateDuplicateSpecNameMap(
        ImmutableArray<OpenApiFileInput> files)
        => files
            .GroupBy(f => f.SpecName, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .ToDictionary(
                g => g.Key,
                g => g
                    .Select(f => f.Path)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.Ordinal);

    private static readonly Version[] _knownVersions =
    [
        KnownOpenApiVersions.V3_0,
        KnownOpenApiVersions.V3_1,
    ];

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="version"/> matches the major/minor of any
    /// entry in <see cref="KnownOpenApiVersions"/>, accepting any patch/build suffix (e.g. 3.0.3 is
    /// considered a known 3.0 version).  Returns <see langword="false"/> for <see langword="null"/> or
    /// any version whose major.minor is not explicitly listed.
    /// </summary>
    private static bool IsKnownVersion(Version? version) =>
        version is not null &&
        Array.Exists(_knownVersions, kv => kv.Major == version.Major && kv.Minor == version.Minor);

    /// <summary>
    /// The ordered list of parsers consulted by <see cref="SelectParser"/>.
    /// The first parser whose <see cref="IOpenApiParser.CanParse"/> returns <see langword="true"/>
    /// is used.  To support a new version with breaking structural changes, prepend a
    /// version-targeted parser here; existing parsers are unmodified.
    /// </summary>
    private static readonly IOpenApiParser[] _parsers =
    [
        new YamlOpenApiParser(),
        new JsonOpenApiParser(),
    ];

    /// <summary>
    /// Detects the serialisation format from the file extension and does a lightweight content
    /// peek for the <c>openapi</c> version field, then returns the first registered parser whose
    /// <see cref="IOpenApiParser.CanParse"/> accepts the resulting <see cref="OpenApiParserRequest"/>.
    /// Returns <see langword="null"/> if no parser accepts the file (caller emits <b>MOA005</b>).
    /// </summary>
    private static IOpenApiParser? SelectParser(string path, string content)
    {
        var format = DetectFormat(path);
        var version = PeekVersion(format, content);
        var request = new OpenApiParserRequest(format, version);
        return Array.Find(_parsers, p => p.CanParse(request));
    }

    private static OpenApiFormat DetectFormat(string path)
    {
        var ext = System.IO.Path.GetExtension(path);
        return ext.ToLowerInvariant() switch
        {
            ".yaml" or ".yml" => OpenApiFormat.Yaml,
            ".json" => OpenApiFormat.Json,
            _ => OpenApiFormat.Unknown
        };
    }

    // Lightweight regexes that extract the raw version string from the openapi field
    // without a full parse.  These run before parser selection and must work on both
    // well-formed and partially malformed documents.
    // RegexOptions.Compiled is intentionally omitted: this generator targets netstandard2.0
    // and runs inside the Roslyn analyzer host, where runtime code-gen (Compiled) can fail.
    // Each pattern is matched at most once per file so uncompiled performance is acceptable.
    private static readonly Regex _yamlVersionPattern =
        new(@"^\s*openapi\s*:\s*[""']?(\d[\d.]*)(?=[""'\s]|$)", RegexOptions.Multiline);

    private static readonly Regex _jsonVersionPattern =
        new(@"""openapi""\s*:\s*""([\d.]+)""");

    private static Version? PeekVersion(OpenApiFormat format, string content)
    {
        var regex = format == OpenApiFormat.Yaml ? _yamlVersionPattern : _jsonVersionPattern;
        var match = regex.Match(content);
        return match.Success && Version.TryParse(match.Groups[1].Value, out var v) ? v : null;
    }

    /// <summary>
    /// Creates a <see cref="Location"/> that points to the start of the given OpenAPI
    /// spec file.  Using a real file location (rather than <see cref="Location.None"/>)
    /// ensures that diagnostics are visible in IDE Error Lists, shown with the correct
    /// filename, and survive the Roslyn incremental-generator analysis cache.
    /// </summary>
    private static Location CreateOpenApiLocation(string filePath)
        => Location.Create(
            filePath,
            textSpan: TextSpan.FromBounds(0, 0),
            lineSpan: new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));

    /// <summary>
    /// Derives the spec name used as a namespace segment from the file path or an explicit override.
    /// When <paramref name="explicitNamespace"/> is non-empty it is returned as-is (callers supply
    /// a valid identifier via the <c>Namespace</c> MSBuild item metadata attribute).
    /// Otherwise the file name (without extension) is converted to PascalCase: hyphens, underscores
    /// and dots are treated as word separators, e.g. <c>payment-api.yaml</c> → <c>PaymentApi</c>.
    /// </summary>
    internal static string DeriveSpecName(string filePath, string? explicitNamespace)
    {
        if (!string.IsNullOrWhiteSpace(explicitNamespace))
            return explicitNamespace!;

        var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return "Api";

        return TypeMapper.ToPascalCase(fileName);
    }

    private const string ComponentParametersPrefix = "#/components/parameters/";

    /// <summary>
    /// Resolves all <c>$ref</c> entries in operation parameter lists against
    /// <see cref="OpenApiDocument.ComponentParameters"/> and returns a new list of operations
    /// with all parameters fully resolved.  The parsed <see cref="OpenApiDocument"/> is never
    /// mutated.  Inline parameters are copied as-is.  If any reference cannot be resolved,
    /// MOA008 is reported for each failure, <paramref name="operations"/> is set to an empty
    /// list, and the method returns <see langword="false"/>.
    /// </summary>
    private static bool TryResolveParameterReferences(
        SourceProductionContext spc,
        OpenApiDocument doc,
        string openApiFilePath,
        out List<OpenApiOperation> operations)
    {
        operations = new List<OpenApiOperation>(doc.Operations.Count);
        var allResolved = true;

        foreach (var op in doc.Operations)
        {
            var resolvedParameters = new List<OpenApiParameter>(op.Parameters.Count);

            foreach (var param in op.Parameters)
            {
                if (param.Reference is null)
                {
                    // Inline parameter — copy as-is.
                    resolvedParameters.Add(param);
                    continue;
                }

                var refValue = param.Reference;

                // Only local #/components/parameters/{name} refs are supported.
                if (!refValue.StartsWith(ComponentParametersPrefix, StringComparison.Ordinal))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.UnresolvedParameterReference,
                        CreateOpenApiLocation(openApiFilePath),
                        refValue, op.OperationId));
                    allResolved = false;
                    continue;
                }

                var paramName = refValue.Substring(ComponentParametersPrefix.Length);
                if (!doc.ComponentParameters.TryGetValue(paramName, out var componentParam))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.UnresolvedParameterReference,
                        CreateOpenApiLocation(openApiFilePath),
                        refValue, op.OperationId));
                    allResolved = false;
                    continue;
                }

                resolvedParameters.Add(componentParam);
            }

            operations.Add(new OpenApiOperation
            {
                OperationId = op.OperationId,
                HttpMethod = op.HttpMethod,
                Route = op.Route,
                Summary = op.Summary,
                Description = op.Description,
                Tags = op.Tags,
                Parameters = MergeParametersByIdentity(resolvedParameters),
                RequestBody = op.RequestBody,
                Responses = op.Responses,
            });
        }

        if (!allResolved)
        {
            operations = [];
            return false;
        }

        return true;
    }

    private static List<OpenApiParameter> MergeParametersByIdentity(IReadOnlyList<OpenApiParameter> parameters)
    {
        var merged = new List<OpenApiParameter>(parameters.Count);
        var indexesByKey = new Dictionary<(string Name, ParameterLocation Location), int>();

        foreach (var parameter in parameters)
        {
            var key = (parameter.Name, parameter.Location);
            if (indexesByKey.TryGetValue(key, out var existingIndex))
            {
                merged[existingIndex] = parameter;
                continue;
            }

            indexesByKey[key] = merged.Count;
            merged.Add(parameter);
        }

        return merged;
    }

    private static string SchemaHintName(string specName)
        => $"MinimalOpenApi/{specName}/Schemas/{specName}.Dtos.g.cs";

    private static string OperationHintName(string specName, string typeName)
        => $"MinimalOpenApi/{specName}/Operations/{specName}.{typeName}.g.cs";

    private static string InfrastructureHintName(string specName, string fileName)
        => $"MinimalOpenApi/{specName}/Infrastructure/{specName}.{fileName}.g.cs";

    private static void GenerateForDocument(
        SourceProductionContext spc,
        OpenApiGenerationInput input,
        ClassInfo[] allClasses)
    {
        var doc = input.Document;
        var rootNamespace = input.RootNamespace;
        var specName = input.SpecName;
        var openApiFilePath = input.Path;
        var schemaId = input.SchemaId;
        var publishAs = input.PublishAs;
        var displayName = input.DisplayName;
        var displayVersion = input.DisplayVersion;

        // Resolve $ref parameter references before code generation; work with the returned
        // normalized operation list so the parsed OpenApiDocument is never mutated.
        if (!TryResolveParameterReferences(spc, doc, openApiFilePath, out var operations))
            return;

        // Build the schema name map once per document and report normalization errors
        // before any code is emitted.
        var schemaNameMap = SchemaNameMap.Build(doc.Schemas.Keys);

        foreach (var unnormalisable in schemaNameMap.UnnormalisableNames)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UnnormalisableSchemaName,
                CreateOpenApiLocation(openApiFilePath),
                unnormalisable));
        }

        foreach (var collision in schemaNameMap.Collisions)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SchemaNameCollision,
                CreateOpenApiLocation(openApiFilePath),
                string.Join(", ", collision.OriginalNames.Select(n => $"'{n}'")),
                collision.NormalisedTypeName));
        }

        // Abort code generation when there are name-level errors: unnormalisable names would
        // produce invalid C# identifiers and collisions would produce duplicate type declarations.
        if (schemaNameMap.HasUnnormalisableNames || schemaNameMap.HasCollisions)
            return;

        var directionality = SchemaDirectionalityAnalysis.Create(
            doc.Schemas,
            operations,
            input.ReadWriteSchemaHandling,
            schemaNameMap);

        // Generate DTOs
        if (doc.Schemas.Count > 0)
        {
            var dtoResult = DtoGenerator.Generate(doc.Schemas, rootNamespace, specName, directionality, schemaNameMap);
            if (!string.IsNullOrWhiteSpace(dtoResult.Source))
                spc.AddSource(SchemaHintName(specName), dtoResult.Source);

            foreach (var conflict in dtoResult.AllOfConflicts.Distinct())
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.AllOfPropertyConflict,
                    CreateOpenApiLocation(openApiFilePath),
                    conflict.SchemaName,
                    conflict.PropertyName));
            }

            foreach (var collision in dtoResult.GeneratedSymbolCollisions.Distinct())
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.GeneratedSymbolCollision,
                    CreateOpenApiLocation(openApiFilePath),
                    collision));
            }
        }

        // Discover handlers and customizers, emit diagnostics
        var handlers = new List<DiscoveredImplementation>();
        var customizers = new List<DiscoveredImplementation>();

        foreach (var op in operations)
        {
            var handlerBase = TypeMapper.HandlerClassName(op.OperationId);
            var customizerBase = TypeMapper.RegistrationClassName(op.OperationId);

            // Generate handler base
            var handlerConflicts = new List<MinimalOpenAPI.Generator.CodeGen.AllOfPropertyConflict>();
            var handlerMultipartShapes = new List<MinimalOpenAPI.Generator.CodeGen.MultipartUnsupportedShape>();
            var handlerSource = HandlerBaseGenerator.Generate(op, rootNamespace, specName, directionality, doc.Schemas, handlerConflicts, handlerMultipartShapes);
            spc.AddSource(OperationHintName(specName, handlerBase), handlerSource);

            foreach (var conflict in handlerConflicts.Distinct())
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.AllOfPropertyConflict,
                    CreateOpenApiLocation(openApiFilePath),
                    conflict.SchemaName,
                    conflict.PropertyName));
            }

            foreach (var shape in handlerMultipartShapes.Distinct())
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedMultipartFormShape,
                    CreateOpenApiLocation(openApiFilePath),
                    shape.PropertyName,
                    shape.FormRecordTypeName));
            }

            // Generate registration customizer base
            var customizerSource = RegistrationCustomizerGenerator.Generate(op, rootNamespace, specName);
            spc.AddSource(OperationHintName(specName, customizerBase), customizerSource);

            // Discover handler implementations
            var handlerImpls = allClasses
                .Where(c => c.BaseTypeNames.Contains(handlerBase))
                .ToList();

            switch (handlerImpls.Count)
            {
                case 0:
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.MissingHandlerImplementation,
                        CreateOpenApiLocation(openApiFilePath),
                        $"{rootNamespace}.{specName}.Endpoints.{handlerBase}"));
                    break;
                case 1:
                    handlers.Add(new DiscoveredImplementation
                    {
                        BaseName = handlerBase,
                        FullName = handlerImpls[0].FullName
                    });
                    break;
                default:
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateHandlerImplementation,
                        CreateOpenApiLocation(openApiFilePath),
                        handlerBase,
                        string.Join(", ", handlerImpls.Select(h => h.FullName))));
                    break;
            }

            // Discover customizer implementations
            var customizerImpls = allClasses
                .Where(c => c.BaseTypeNames.Contains(customizerBase))
                .ToList();

            switch (customizerImpls.Count)
            {
                case 0:
                    break; // Optional, none is fine
                case 1:
                    customizers.Add(new DiscoveredImplementation
                    {
                        BaseName = customizerBase,
                        FullName = customizerImpls[0].FullName
                    });
                    break;
                default:
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateRegistrationCustomizerImplementation,
                        CreateOpenApiLocation(openApiFilePath),
                        customizerBase,
                        string.Join(", ", customizerImpls.Select(c => c.FullName))));
                    break;
            }
        }

        // Generate DI registration
        var diSource = DependencyInjectionRegistrationGenerator.Generate(
            operations,
            handlers,
            customizers,
            rootNamespace,
            specName,
            schemaId,
            System.IO.Path.GetFileName(openApiFilePath),
            publishAs,
            displayName,
            displayVersion);
        spc.AddSource(InfrastructureHintName(specName, "DependencyInjection"), diSource);

        // Generate endpoint mapping
        var mappingSource = EndpointMappingGenerator.Generate(operations, customizers, rootNamespace, specName, directionality);
        spc.AddSource(InfrastructureHintName(specName, "EndpointMapping"), mappingSource);
    }

    private static bool TryParseReadWriteSchemaHandling(string? value, out ReadWriteSchemaHandling handling)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            handling = ReadWriteSchemaHandling.Auto;
            return true;
        }

        if (string.Equals(value, "Ignore", StringComparison.OrdinalIgnoreCase))
        {
            handling = ReadWriteSchemaHandling.Ignore;
            return true;
        }

        if (string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            handling = ReadWriteSchemaHandling.Auto;
            return true;
        }

        if (string.Equals(value, "Split", StringComparison.OrdinalIgnoreCase))
        {
            handling = ReadWriteSchemaHandling.Split;
            return true;
        }

        handling = default;
        return false;
    }

    /// <summary>Information about a class discovered via syntax analysis.</summary>
    private sealed record ClassInfo(
        string FullName,
        string Name,
        List<string> BaseTypeNames);

    /// <summary>OpenAPI file metadata collected from additional files and item metadata.</summary>
    private sealed record OpenApiFileInput(
        string Content,
        string Path,
        string SpecName,
        string SchemaId,
        string? PublishAs,
        string? DisplayName,
        string? DisplayVersion,
        string? ReadWriteSchemaHandling);

    /// <summary>Parsed file state, including parser/extension failures used for diagnostics.</summary>
    private sealed record ParsedOpenApiFile(
        OpenApiDocument? Document,
        string Path,
        string RootNamespace,
        string SpecName,
        string SchemaId,
        string? PublishAs,
        string? DisplayName,
        string? DisplayVersion,
        string? ReadWriteSchemaHandling,
        string? ParseError,
        string? UnsupportedExtension)
    {
        public bool CanGenerate =>
            Document is not null &&
            ParseError is null &&
            UnsupportedExtension is null;

        public OpenApiGenerationInput ToGenerationInput(ReadWriteSchemaHandling readWriteSchemaHandling) =>
            new(
                Document!,
                Path,
                RootNamespace,
                SpecName,
                SchemaId,
                PublishAs,
                DisplayName,
                DisplayVersion,
                readWriteSchemaHandling);
    }

    /// <summary>Generation-ready non-null OpenAPI input.</summary>
    private sealed record OpenApiGenerationInput(
        OpenApiDocument Document,
        string Path,
        string RootNamespace,
        string SpecName,
        string SchemaId,
        string? PublishAs,
        string? DisplayName,
        string? DisplayVersion,
        ReadWriteSchemaHandling ReadWriteSchemaHandling);

    /// <summary>Combined generator pipeline state consumed by source output registration.</summary>
    private sealed record GeneratorPipelineInput(
        ParsedOpenApiFile ParsedFile,
        IReadOnlyList<ClassInfo> Classes,
        IReadOnlyDictionary<string, string[]> DuplicatesBySpecName);
}