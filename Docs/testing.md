# Testing Guide

## Running Tests

From the repository root:

```powershell
dotnet test Zeron.sln -c Release
```

Test project: `ZeronTests` (MSTest).

## Test Categories

| Area | Location | Description |
|------|----------|-------------|
| Unit tests | `ZeronTests/ZCore/`, `ZeronTests/ZServers/`, `ZeronTests/Samples/` | Core utilities, auth, task dispatch, alerts, sample gate plugin |
| Server E2E | `ZeronTests/Server/ServerE2ETests.cs` | Full HTTP flow via `WebApplicationFactory` |
| Agent diagnostics | `ZeronTests/ZServers/AgentDiagnosticServerTests.cs` | Connection state logic |

## E2E Integration Tests

E2E tests use `ZeronServerWebApplicationFactory` (`WebApplicationFactory<Program>`) with:

- Environment: `Testing` (skips NetMQ PUB and background workers)
- Isolated SQLite database per factory instance
- In-memory configuration for API keys and admin credentials

### Covered Scenarios

1. **HeartbeatWithoutApiKeyReturnsUnauthorized** — rejects missing agent key
2. **AgentHeartbeatTaskDispatchFlow** — heartbeat → online → create task → pending task → report result
3. **AgentDiagnosticsReturnsHealthyState** — diagnostic API after successful heartbeat

### Example

```powershell
dotnet test ZeronTests/ZeronTests.csproj --filter "FullyQualifiedName~ServerE2ETests"
```

## EF Core Migrations

Migrations live in `Zeron.Server/Data/Migrations/`.

### Add a migration

```powershell
cd Zeron.Server
dotnet ef migrations add MigrationName --project Zeron.Server.csproj
```

### Apply migrations

Automatic on server startup, or manually:

```powershell
dotnet ef database update --project Zeron.Server.csproj
```

Design-time factory: `ZeronServerDbContextFactory` uses `appsettings.json` for `Zeron:DatabasePath`.

### Upgrading from pre-migration databases

If you previously ran `Zeron.Server` before EF migrations were introduced, the existing SQLite file may have been created with `EnsureCreated()`. On startup you might see:

`SQLite Error 1: 'table "Agents" already exists'.`

The server now detects this legacy state and baselines the database automatically. Restart the server after updating.

If problems persist, back up and remove the database file (default `Data/zeron-server.db`) and restart to create a fresh schema.

## CI

Workflow: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)

| Job | Purpose |
|-----|---------|
| `build-and-test` | Restore, build, test on `windows-latest` |
| `publish` | Publish `Zeron.Server` + `Zeron.Demand` (win-x64), zip, upload artifacts |

Artifacts (retention 30 days):

- `zeron-server-win-x64` → `zeron-server-win-x64.zip`
- `zeron-agent-win-x64` → `zeron-agent-win-x64.zip`

Triggers: push/PR to `main`, `master`, or `develop`.

E2E tests do not require external services; each run uses a temporary SQLite file.
