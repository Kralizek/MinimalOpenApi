# Samples

This directory contains focused, single-purpose samples rather than one large kitchen-sink application.
Each sample demonstrates one clear capability area so you can find exactly what you need.

## Sample catalog

| Sample | When to open it |
|--------|-----------------|
| [BasicTodo](BasicTodo/README.md) | **Start here.** Minimal contract-first workflow — a simple Todo API with list, create, get, and delete. |
| [SchemaPublishing](SchemaPublishing/README.md) | When you need `PublishAs`, `MapOpenApiSchemas()`, or Swagger UI wiring. |
| [Parameters](Parameters/README.md) | Path, query, header, cookie, and component `$ref` parameters; defaults and format annotations. |
| [SchemaShapes](SchemaShapes/README.md) | DTO generation from schema shapes: enums, `allOf`, `readOnly`/`writeOnly`, `additionalProperties`, `DateOnly`. |
| [ResponseResults](ResponseResults/README.md) | Typed result unions (`Created<T>`, `Ok<T>`, `NoContent`) and `application/problem+json` wrappers. |
| [GeneratedFiles](GeneratedFiles/README.md) | `EmitCompilerGeneratedFiles` — how to see and inspect the C# source that MinimalOpenAPI generates. |
| [SmokeTest](SmokeTest/README.md) | CI/package-consumption validation via `PackageReference`. Not the recommended learning sample. |

## Where to start

Open [BasicTodo](BasicTodo/README.md) first. Once you understand the contract-first workflow,
browse the other samples in any order depending on what you want to learn.
