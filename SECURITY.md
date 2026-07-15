# Security Policy

## Supported versions

Security fixes are provided for the latest stable minor release and, when relevant, the latest published prerelease.

| Version | Supported |
|---|---|
| Latest stable minor | ✅ |
| Latest prerelease | Best effort |
| Older releases | ❌ |

For the 1.0 line, this means the latest `1.0.x` release receives security fixes until a newer supported minor or major version is published.

## Reporting a vulnerability

Please **do not** disclose security vulnerabilities in public GitHub issues, pull requests, discussions, or comments.

Use GitHub's private vulnerability reporting form:

https://github.com/Kralizek/MinimalOpenApi/security/advisories/new

Include:

- the affected MinimalOpenAPI version;
- the affected .NET and ASP.NET Core versions;
- reproduction steps or a minimal reproduction;
- the expected security impact;
- any known mitigations.

Do not include live credentials, access tokens, private keys, personal data, or other sensitive material that is not necessary to assess the report.

If the private reporting form is unavailable, contact the maintainer through their GitHub profile without posting vulnerability details publicly.

## Response expectations

MinimalOpenAPI is maintained in spare time. Reports should receive an initial acknowledgement within a few business days. Remediation timing depends on severity, reproducibility, and the complexity of producing and validating a safe fix.
