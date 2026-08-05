# Deployment Guide

This guide covers deploying **Zeron.Server** (central management) and **Zeron.Demand** (Windows agents) in a production environment.

## Prerequisites

- Windows Server or Windows 11 for agents
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (build) and runtime on target machines
- Network access between agents and the server (HTTP + optional NetMQ PUB port)

## 1. Build

### From CI artifacts (recommended)

GitHub Actions CI publishes framework-dependent **win-x64** zip packages after tests pass:

| Artifact | Contents |
|----------|----------|
| `zeron-server-win-x64` | `Zeron.Server` publish output |
| `zeron-agent-win-x64` | `Zeron.Demand` publish output + `App.Sample.config` |

Download from the workflow run → **Artifacts**, extract, then configure before first start. Each zip includes `BUILD.txt` with commit SHA.

### Local build

From the repository root:

```powershell
dotnet build Zeron.sln -c Release
dotnet test Zeron.sln -c Release
dotnet publish Zeron.Server/Zeron.Server.csproj -c Release -r win-x64 --self-contained false -o ./publish/server
dotnet publish Zeron.Demand/Zeron.Demand.csproj -c Release -r win-x64 --self-contained false -o ./publish/agent
Copy-Item ./Zeron.Demand/App.Sample.config ./publish/agent/App.config -Force
```

Target machines need the [.NET 10 Windows runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (framework-dependent packages).

## 2. Configure Zeron.Server

1. Copy `appsettings.Production.template.json` to `appsettings.Production.json` in the publish folder (or set values via environment variables).
2. Set required secrets before first run:

| Setting | Description |
|---------|-------------|
| `Zeron:AgentApiKey` | Shared secret for agent HTTP API (`X-Zeron-Agent-Key` header) |
| `Zeron:JwtSecret` | JWT signing key (minimum 32 characters) |
| `Zeron:DefaultAdminPassword` | Initial dashboard admin password (change after first login) |

Example environment variables (PowerShell):

```powershell
$env:Zeron__AgentApiKey = "your-secure-agent-key"
$env:Zeron__JwtSecret = "your-secure-jwt-secret-min-32-chars"
$env:Zeron__DefaultAdminPassword = "ChangeMeNow!"
$env:ASPNETCORE_ENVIRONMENT = "Production"
```

3. Ensure the database directory exists. Default path: `Data/zeron-server.db` (relative to the server working directory).

### Database Migrations

On startup, `Zeron.Server` applies EF Core migrations automatically via `DatabaseMigrationServer`. For manual migration during deployment:

```powershell
cd Zeron.Server
dotnet ef database update --project Zeron.Server.csproj
```

Design-time factory: `ZeronServerDbContextFactory` reads `Zeron:DatabasePath` from configuration.

## 3. Run Zeron.Server

```powershell
cd publish/server
dotnet Zeron.Server.dll --urls "http://0.0.0.0:5000"
```

For production, run behind IIS, Windows Service wrapper, or a process manager. Open firewall ports:

| Port | Purpose |
|------|---------|
| 5000 (configurable) | HTTP — Dashboard, REST API, agent heartbeat |
| 6000 (default) | NetMQ PUB — push commands to agents |

## 4. Configure Zeron.Demand (Agent)

Edit `App.config` in the agent publish folder:

```xml
<add key="server_enabled" value="true" />
<add key="server_url" value="http://your-server:5000" />
<add key="server_api_key" value="your-secure-agent-key" />
```

Install and start the Windows Service:

```powershell
sc create ZeronDemand binPath= "C:\path\to\publish\agent\Zeron.Demand.exe"
sc start ZeronDemand
```

## 5. Verify Deployment

1. Check health probes: `GET /health` and `GET /ready` should return `"status":"healthy"`.
2. Open the Dashboard at `http://your-server:5000` and sign in with the default admin account.
3. Complete the forced password change page (required on first login / while using the default password).
4. Confirm the agent appears on **Agents** with connection state **healthy**.
5. Create a test task (e.g. `HealthCheck`) targeting the agent.
6. Check **Events** and **Alerts** for operational data.
7. On **Users**, create Operator/Viewer accounts as needed.
8. For package deploy: ensure each agent has a populated `managed_packages` SQLite catalog, then use Dashboard **Packages → Deploy Package**. Task status should go `running` then `completed`/`failed` after install finishes.

See [Agent Connection Guide](./agent-connection.md) if agents stay offline or stale.

## 6. Transport Security (CURVE + HMAC)

Production template enables CURVE and HMAC. Dev defaults leave them off for easier local work.

### Enable NetMQ CURVE (command channel)

1. Set Server `Zeron:CurveEnabled=true` and start once — keys are created at `CurveSecretKeyPath` / `CurvePublicKeyPath`.
2. Copy `curve-server.public` to each agent (e.g. `Resource/curve-server.public`).
3. On agents:
   ```xml
   <add key="zmq_sub_curve_enabled" value="true" />
   <add key="zmq_sub_curve_server_public_key_file" value="Resource/curve-server.public" />
   ```
4. Restart Server, then agents. Both sides must enable CURVE or the SUB connect will fail.

**CURVE key rotation:** generate a new Server key pair (delete old files and restart, or replace both files), distribute the new `.public` to agents, restart Server, then restart agents.

### Enable HTTP HMAC

1. Server: `Zeron:AgentHmacRequired=true`
2. Agent: `server_hmac_enabled=true`
3. Keep `server_api_key` / `AgentApiKey` in sync (HMAC uses the same secret)

**API key rotation:** set Server `AgentApiKey` to `oldKey|newKey`, update agents to `newKey`, then remove `oldKey` from Server.

### HTTPS for agents

Prefer a reverse proxy terminating TLS. Set `RequireHttpsAgents=true` when agents call HTTPS directly, or when the proxy sets `X-Forwarded-Proto: https`. If the proxy speaks plain HTTP to Kestrel, leave `RequireHttpsAgents=false`.

Local Agent REP/PUB ports remain plaintext for `Zeron.Client` and local tooling.

## 7. Security Checklist

- Change default admin password immediately after first login
- Use strong, unique `AgentApiKey` and `JwtSecret`
- Enable `CurveEnabled` + agent `zmq_sub_curve_enabled` in production
- Enable `AgentHmacRequired` + agent `server_hmac_enabled` in production
- Prefer HTTPS reverse proxy (IIS, nginx, Caddy) in production
- Restrict NetMQ PUB port to agent subnet only
- Enable alert email (`Zeron:AlertEmailEnabled`) for offline notifications
- Confirm Dashboard **Transport Security** shows `hardened` after production config

The home Dashboard panel reads server settings only; agents still need matching CURVE/HMAC keys.
