# GitHub Repository Metadata

GitHub repository settings (description, topics, and homepage URL) cannot be set through
files in the repository. This document records the recommended values so that a maintainer
with admin access can apply them through the repository **Settings → General** page or via
the GitHub API / CLI.

---

## Description

```
Contract-first OpenAPI framework for ASP.NET Core Minimal APIs — author an OpenAPI spec and a Roslyn source generator emits DTOs, handler base classes, and endpoint mapping at build time.
```

*(GitHub enforces a 350-character limit on repository descriptions.)*

---

## Topics

```
dotnet
aspnetcore
minimal-api
openapi
source-generator
roslyn
csharp
code-generation
```

Apply them via the **Settings → General → Topics** field, or with the GitHub CLI:

```bash
gh repo edit Kralizek/MinimalOpenApi \
  --add-topic dotnet \
  --add-topic aspnetcore \
  --add-topic minimal-api \
  --add-topic openapi \
  --add-topic source-generator \
  --add-topic roslyn \
  --add-topic csharp \
  --add-topic code-generation
```

---

## Homepage

No dedicated website currently exists. Leave the homepage field blank, or set it to the
primary NuGet package page once a stable release is published:

```
https://www.nuget.org/packages/MinimalOpenAPI
```

---

## Notes

- The description and topics above reflect the current **pre-release** state of the project
  (`1.0.0-alpha`). They do not overstate maturity or scope.
- Revisit these values when a `1.0.0` stable release is tagged to ensure they remain
  accurate.
