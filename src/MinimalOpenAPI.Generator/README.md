# MinimalOpenAPI.Generator

Roslyn incremental source generator for the [MinimalOpenAPI](https://github.com/Kralizek/MinimalOpenApi) contract-first framework.

At build time this generator reads your OpenAPI (YAML) spec and produces:

- **DTO records** for every schema defined in the `components/schemas` section.
- **Abstract handler base classes** for every operation, with strongly-typed parameters and return types.
- **DI registration** — a `[ModuleInitializer]` that registers all handlers with the DI container.
- **Endpoint mapping** — a `MapEndpoints` extension method that wires each operation to its route.

The generator is distributed as a Roslyn analyzer; it runs entirely at compile time and produces no runtime overhead.

This package is included transitively when you reference [`MinimalOpenAPI`](https://www.nuget.org/packages/MinimalOpenAPI).
You only need to reference it directly when you want to use the generator without the runtime package.

For full documentation and examples, visit the [MinimalOpenAPI repository](https://github.com/Kralizek/MinimalOpenApi).
