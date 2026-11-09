# Moving an application or an agent workflow from 1.0 to 1.1

1. Read the installed package version and existing handler signatures before upgrading. Do not silently change versions as a side effect of feature development.
2. Compare the authored contract against the application's actual middleware and handlers. Keep every observable status in the OpenAPI responses.
3. Use `OpenApi.ExcludeStatusCodesFromHandlers` and `MinimalOpenApiHandler` with `Include`, optional/required `OpenApiFile`, `IncludeStatusCodes`, and `ExcludeStatusCodes` to model response ownership.
4. Generated handlers may change signature after response filtering. Rebuild, inspect generated source and update overrides.
5. Operations with more than six distinct effective result types use nested `Results<...>` (not `IResult` simply due to generic arity); implement conversions following the generated signature.
6. New MOA015, MOA016 and MOA017 diagnostics catch invalid handler-response configuration, duplicate operation IDs and unsupported/unresolved schema references. Repair the contract or item metadata.
7. Run the application's build/tests and exercise responses produced by middleware separately from business-handler responses.

Reference [1.1 release notes](../../../CHANGELOG.md#110) and [consumer guide](../../../docs/consumer-agents.md). No generated file should be hand-edited during migration.
