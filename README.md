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

`Uvse.Application` contains MediatR commands/queries, DTOs, policies, and app-facing contracts.

`Uvse.Domain` defines tenant-scoped entities, plugin abstractions, and synthesis primitives.

`Uvse.Infrastructure` implements EF Core, provider registry, the mock Jira plugin, caching hooks, and tenant-aware services.
