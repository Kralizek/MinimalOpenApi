# Benchmarks

This directory contains the BenchmarkDotNet suite used to compare the
MinimalOpenAPI-based implementation against a hand-written Minimal API
implementation.

## Directory layout

- `Benchmark/`
  - Benchmark runner project (`Benchmark.csproj`)
  - Defines scenarios in `TodoApiBenchmarks.cs`
  - Entry point in `Program.cs`
- `WithMinimalOpenAPI/`
  - Project under test using MinimalOpenAPI-generated endpoints
  - OpenAPI contract in `todo.yaml`
- `WithoutMinimalOpenApi/`
  - Project under test using hand-written Minimal API endpoints
  - Mirrors the Todo behavior for side-by-side comparison

The benchmark suite executes both projects in-process via
`WebApplicationFactory` and compares equivalent endpoint workflows.

## Run benchmarks

From the repository root:

```bash
dotnet run --project benchmarks/Benchmark/Benchmark.csproj -c Release
```

## Latest run results

Environment:

- BenchmarkDotNet: `0.15.8`
- OS: `Windows 11 (24H2)`
- CPU: `Intel Core Ultra 9 185H`
- Runtime: `.NET 10.0.6`

### Create

The create scenario issues `POST /todos` and measures request binding,
validation, serialization, and item creation in storage.

| Implementation        | Mean     | Error   | Ratio | Allocated |
| --------------------- | -------- | ------- | ----- | --------- |
| WithMinimalOpenApi    | 125.9 us | 2.78 us | 1.13  | 26.55 KB  |
| WithoutMinimalOpenApi | 116.0 us | 6.76 us | 1.04  | 26.04 KB  |

### Get by id

This scenario creates an item and calls `GET /todos/{id}` to benchmark
path parameter binding, lookup, and response generation.

| Implementation        | Mean     | Error   | Ratio | Allocated |
| --------------------- | -------- | ------- | ----- | --------- |
| WithMinimalOpenApi    | 163.4 us | 3.27 us | 1.01  | 37.59 KB  |
| WithoutMinimalOpenApi | 161.9 us | 3.16 us | 1.00  | 36.75 KB  |

### List completed

This scenario creates an item, marks it completed with `PUT /todos/{id}`,
then runs `GET /todos?completed=true` to measure filtered listing.

| Implementation        | Mean     | Error   | Ratio | Allocated |
| --------------------- | -------- | ------- | ----- | --------- |
| WithMinimalOpenApi    | 219.6 us | 4.30 us | 0.98  | 53.88 KB  |
| WithoutMinimalOpenApi | 224.2 us | 4.48 us | 1.00  | 52.98 KB  |

### Update by id

This scenario creates an item and updates it via `PUT /todos/{id}` to
measure mutation-path handling plus response generation.

| Implementation        | Mean     | Error   | Ratio | Allocated |
| --------------------- | -------- | ------- | ----- | --------- |
| WithMinimalOpenApi    | 182.6 us | 3.61 us | 1.05  | 42.28 KB  |
| WithoutMinimalOpenApi | 174.8 us | 3.46 us | 1.00  | 41.6 KB   |

### Delete by id

This scenario creates an item and removes it through `DELETE /todos/{id}`
to benchmark delete-path processing.

| Implementation        | Mean     | Error   | Ratio | Allocated |
| --------------------- | -------- | ------- | ----- | --------- |
| WithMinimalOpenApi    | 134.1 us | 4.59 us | 1.10  | 26.55 KB  |
| WithoutMinimalOpenApi | 122.3 us | 2.44 us | 1.00  | 26.04 KB  |

### Full CRUD flow

This end-to-end scenario performs create, get, update, list-completed,
and delete in sequence to approximate a realistic client workflow.

| Implementation        | Mean     | Error   | Ratio | Allocated |
| --------------------- | -------- | ------- | ----- | --------- |
| WithMinimalOpenApi    | 269.2 us | 5.33 us | 0.96  | 65.04 KB  |
| WithoutMinimalOpenApi | 281.7 us | 5.63 us | 1.00  | 63.86 KB  |

Notes:

- `Ratio` is relative to each scenario baseline (`WithoutMinimalOpenApi`).
- `Error` is half of the 99.9% confidence interval from BenchmarkDotNet.
- Lower mean and allocation are better.
- Re-run on your machine before drawing conclusions; results are hardware
  and environment dependent.
