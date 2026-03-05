# Production Deployment Guide

This document explains how to deploy UVSE to a production environment step by step.

## 1. Prerequisites

Before deploying, make sure you have:

- A PostgreSQL instance for the application database
- A Redis instance for distributed cache
- A production OIDC identity provider that issues JWTs with:
  - `tenant_id`
  - user identity claim
  - role claims such as `TenantAdmin`, `StandardUser`, `ProjectManager`, and `DataSourceManager`
- Durable shared storage for ASP.NET Core Data Protection keys
  - Example: mounted persistent volume, cloud file share, or equivalent shared filesystem
- A container runtime or VM host for the application
- TLS termination in front of the app
- Network controls so only the API entry point is public

## 2. Provision Infrastructure

Use the infrastructure-as-code assets in [infra/terraform/main.tf](/Users/katesamuels/Documents/projects/wrap-api/infra/terraform/main.tf) as the starting point.

Provision at minimum:

- Resource group / project boundary
- Private network
- Web application firewall
- Application host
- PostgreSQL
- Redis
- Persistent shared storage for Data Protection keys
- Secret store for connection strings and auth settings

Recommended production shape:

- Run the API behind a reverse proxy or managed ingress
- Keep PostgreSQL and Redis private
- Restrict inbound traffic to HTTPS only
- Place the app behind the WAF

## 3. Build the Application

From the repo root:

```bash
dotnet restore Uvse.slnx
dotnet build Uvse.slnx -c Release
dotnet test Uvse.slnx -c Release
```

Publish the web app:

```bash
dotnet publish src/Uvse.Web/Uvse.Web.csproj -c Release -o ./publish
```

Deploy the contents of `./publish` to your target host, or package it into a container image.

## 4. Configure Production Settings

Do not rely on the default `appsettings.json` values in production. Supply configuration through environment variables, secret management, or production config files.

Required settings:

- `ConnectionStrings__UvseDb`
- `ConnectionStrings__Redis`
- `Authentication__Authority`
- `Authentication__Audience`
- `Security__DataProtection__KeyRingPath`

Optional settings:

- `Features__provider-summary`
- `Features__Tenants__{tenantId}__provider-summary`
- provider-specific LLM credentials and model configuration, if non-template adapters are introduced

Example environment variables:

```bash
ConnectionStrings__UvseDb=Host=prod-postgres;Port=5432;Database=uvse;Username=uvse;Password=REDACTED
ConnectionStrings__Redis=prod-redis:6379
Authentication__Authority=https://login.example.com/
Authentication__Audience=uvse-api
Security__DataProtection__KeyRingPath=/var/uvse/dataprotection
Features__provider-summary=false
Features__Tenants__11111111-1111-1111-1111-111111111111__provider-summary=true
```

## 4A. Build and Run a Container Image

Build the image from the repo root:

```bash
docker build -t uvse-web:latest .
```

Run it locally against reachable PostgreSQL and Redis instances:

```bash
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__UvseDb="Host=host.docker.internal;Port=5432;Database=uvse;Username=uvse;Password=uvse" \
  -e ConnectionStrings__Redis="host.docker.internal:6379" \
  -e Authentication__Authority="https://login.example.com/" \
  -e Authentication__Audience="uvse-api" \
  -e Security__DataProtection__KeyRingPath="/var/uvse/dataprotection" \
  -v $(pwd)/.keys:/var/uvse/dataprotection \
  uvse-web:latest
```

Notes:

- The image is defined in [Dockerfile](/Users/katesamuels/Documents/projects/wrap-api/Dockerfile)
- In production, mount durable shared storage at `/var/uvse/dataprotection`
- Replace `host.docker.internal` with actual service endpoints in Linux/container platforms

## 5. Configure Data Protection Key Storage

This step is mandatory in production.

The application encrypts plugin settings using ASP.NET Core Data Protection. All app instances must share the same key ring or encrypted plugin settings may become unreadable after restart or scale-out.

Requirements:

- `Security__DataProtection__KeyRingPath` must point to durable storage
- The directory must exist or be creatable by the app
- All app instances must mount the same location
- Back up this key ring

Example:

- `/var/uvse/dataprotection`
- mounted shared volume

## 6. Configure Authentication

The app is a resource server and expects JWT bearer authentication.

Your identity provider must:

- issue JWTs for `Authentication__Audience`
- expose discovery metadata at `Authentication__Authority`
- include a valid `tenant_id` claim
- include a user identity claim
- include role claims for authorization

Required roles:

- `TenantAdmin`
- `StandardUser`
- `ProjectManager`
- `DataSourceManager`

Without these claims, the API will reject requests.

## 7. Prepare the Database

This repo contains EF Core migrations for the current schema.

Before production launch:

1. Review the checked-in migrations.
2. Apply them to the target database as part of deployment.
3. Treat future schema changes as migration-backed releases only.

Current schema includes at least:

- `tenant_plugins`
- `generated_summaries`
- `projects`
- `project_users`
- `project_datasources`
- `datasources`
- `datasource_users`

Apply migrations during deployment with:

```bash
dotnet ef database update \
  --project src/Uvse.Infrastructure/Uvse.Infrastructure.csproj \
  --startup-project src/Uvse.Web/Uvse.Web.csproj
```

## 8. Deploy the Application

Typical deployment flow:

1. Provision infrastructure.
2. Provision secrets and config.
3. Build and publish the application.
4. Deploy the app artifact or container image.
5. Mount shared Data Protection key storage.
6. Apply database migrations.
7. Start the application.
8. Verify health and auth flows.

If using containers, ensure the container:

- exposes the application port
- mounts the Data Protection key directory
- receives the required environment variables
- can reach PostgreSQL, Redis, and the OIDC authority

## 8A. Kubernetes Deployment Example

A minimal manifest set is included under [infra/k8s](/Users/katesamuels/Documents/projects/wrap-api/infra/k8s).

Files:

- [namespace.yaml](/Users/katesamuels/Documents/projects/wrap-api/infra/k8s/namespace.yaml)
- [configmap.yaml](/Users/katesamuels/Documents/projects/wrap-api/infra/k8s/configmap.yaml)
- [secret.yaml](/Users/katesamuels/Documents/projects/wrap-api/infra/k8s/secret.yaml)
- [pvc.yaml](/Users/katesamuels/Documents/projects/wrap-api/infra/k8s/pvc.yaml)
- [deployment.yaml](/Users/katesamuels/Documents/projects/wrap-api/infra/k8s/deployment.yaml)
- [service.yaml](/Users/katesamuels/Documents/projects/wrap-api/infra/k8s/service.yaml)
- [ingress.yaml](/Users/katesamuels/Documents/projects/wrap-api/infra/k8s/ingress.yaml)

Before applying them:

1. Replace the image in `deployment.yaml`
2. Replace example secrets in `secret.yaml`
3. Replace `uvse.example.com` in `ingress.yaml`
4. Make sure the PVC storage class supports `ReadWriteMany`, or adjust the storage strategy
5. Ensure the ingress controller and TLS secret exist

Apply the manifests:

```bash
kubectl apply -f infra/k8s/namespace.yaml
kubectl apply -f infra/k8s/configmap.yaml
kubectl apply -f infra/k8s/secret.yaml
kubectl apply -f infra/k8s/pvc.yaml
kubectl apply -f infra/k8s/deployment.yaml
kubectl apply -f infra/k8s/service.yaml
kubectl apply -f infra/k8s/ingress.yaml
```

Verify rollout:

```bash
kubectl -n uvse get pods
kubectl -n uvse rollout status deployment/uvse-web
kubectl -n uvse get ingress
```

## 9. Validate the Deployment

After startup, verify:

### Health

```bash
curl https://your-api.example.com/health
```

Expected result:

- HTTP 200

### Authenticated API access

Test with a valid JWT containing:

- `tenant_id`
- correct `aud`
- required roles

Then call:

- `POST /api/admin/plugins/enable`
- `POST /api/summaries/projects`
- `POST /api/summaries/datasources`
- `POST /api/summaries/providers`

Confirm:

- `TenantAdmin` can enable plugins
- allow-listed `ProjectManager` users can generate project summaries
- allow-listed `DataSourceManager` users can generate datasource summaries
- allow-listed project users can retrieve project summaries
- allow-listed datasource users can retrieve only datasource summaries they requested
- invalid or missing tokens return `401`
- authenticated users without permission return `403`

## 10. Enable a Plugin Safely

To enable a provider in production:

1. Authenticate as a `TenantAdmin`
2. Submit provider settings in the request body
3. Confirm the plugin is enabled for the correct tenant

Important:

- If you omit `Settings` for an existing plugin, the app preserves the current encrypted settings
- Plugin settings are encrypted before persistence

Example request body:

```json
{
  "providerKey": "jira-mock",
  "settings": {
    "baseUrl": "https://jira.example.com",
    "apiToken": "REDACTED"
  }
}
```

## 11. Observability and Operations

The app emits:

- structured JSON logs
- OpenTelemetry traces
- OpenTelemetry metrics
- correlation IDs

In production, route telemetry to your observability platform instead of relying only on console output.

At minimum:

- collect application logs
- collect request traces
- collect RED metrics
- alert on 5xx rate, auth failure spikes, and startup failures

## 12. Provider and Summary Configuration Notes

Summary generation now accepts an `llmProvider` and optional `llmModel` on request payloads.

Current built-in provider keys:

- `template`
- `openai`
- `gemini`
- `claude`
- `copilot`

At present these keys share the same template-backed implementation, which means no external LLM API calls are made yet. Before a production rollout that depends on a non-template provider, add and validate the corresponding infrastructure adapter and its credential/configuration path.

## 13. Security Checklist

Before go-live, confirm:

- HTTPS is enforced externally
- WAF is enabled
- PostgreSQL and Redis are not publicly exposed
- Data Protection keys are persisted and backed up
- secrets are not stored in source control
- production connection strings are injected securely
- OIDC metadata is served over HTTPS
- JWTs include `tenant_id` and role claims
- logs do not contain provider secrets

## 13. Rollback Plan

Prepare rollback before deployment:

1. Keep the previous application artifact or container image
2. Back up the database
3. Back up the Data Protection key ring
4. If deployment fails, restore the previous artifact and keep the same key ring

Do not rotate or discard the Data Protection keys during rollback unless you are intentionally invalidating all encrypted plugin settings.

## 14. Known Gaps Before Full Production Hardening

The current codebase is a strong starting point, but the following should still be addressed before a mature production launch:

- Add EF Core migrations to the repo
- Add real tenant-backed entitlements instead of config-driven feature flags
- Add a real provider implementation beyond the mock Jira source
- Add secret rotation and credential update workflows
- Add deployment automation in CI/CD
- Add production OpenTelemetry exporters
- Add integration tests that exercise auth and tenant isolation
