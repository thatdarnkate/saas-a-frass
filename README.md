# Universal Verifiable Synthesis Engine

## Planned File Tree

```text
.
├── Directory.Build.props
├── Uvse.slnx
├── README.md
├── docker-compose.yml
├── .devcontainer/
│   └── devcontainer.json
├── docs/
│   ├── adr/
│   │   └── ADR-0001-modular-monolith.md
│   └── openapi/
│       └── uvse.yaml
├── infra/
│   └── terraform/
│       └── main.tf
├── src/
│   ├── Uvse.Web/
│   ├── Uvse.Application/
│   ├── Uvse.Domain/
│   └── Uvse.Infrastructure/
└── tests/
    ├── Uvse.ArchitectureTests/
    └── Uvse.IntegrationTests/
```

## Solution Shape

`Uvse.Web` hosts Minimal APIs, auth, OpenAPI, OpenTelemetry, correlation, and endpoint wiring.

`Uvse.Application` contains MediatR commands/queries, DTOs, authorization checks, and app-facing contracts.

`Uvse.Domain` defines tenant-scoped entities, plugin abstractions, typed provider contracts, and synthesis primitives.

`Uvse.Infrastructure` implements EF Core, provider registry, summary LLM registry, the mock Jira plugin, caching hooks, and tenant-aware services.

## Current API Surface

Implemented route groups:

- `/health`
- `/api/admin/plugins/enable`
- `/api/projects`
- `/api/datasources`
- `/api/summaries/{summaryId}`
- `/api/summaries/projects`
- `/api/summaries/datasources`
- `/api/summaries/providers`

## Current Domain Highlights

- Tenant-scoped `Project` and `Datasource` roots with normalized allow lists and project-to-datasource associations
- Summary generation for providers, projects, and datasources
- Multi-mode summaries with `Executive`, `Detailed`, and `Delta` sections in a single stored summary
- Request-time LLM selection via a provider-agnostic summary interface
- Domain-specific provider contracts for `IDocument`, `IMail`, `ICommunication`, and `IWorkManagement`
