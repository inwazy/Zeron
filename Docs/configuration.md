# Configuration Reference

## Zeron.Server (`appsettings.json` / `Zeron` section)

| Key | Default | Description |
|-----|---------|-------------|
| `DatabasePath` | `Data/zeron-server.db` | SQLite database file path |
| `CommandPubAddr` | `tcp://*:6000` | NetMQ PUB bind address for agent commands |
| `AgentApiKey` | (dev key) | Shared agent secret (`X-Zeron-Agent-Key`); support `old\|new` for rotation |
| `CurveEnabled` | `false` | Enable NetMQ CURVE on command PUB |
| `CurveSecretKeyPath` | `Data/curve-server.secret` | Server CURVE secret key (32 raw bytes) |
| `CurvePublicKeyPath` | `Data/curve-server.public` | Server CURVE public key (copy to agents) |
| `AgentHmacRequired` | `false` | Require `X-Zeron-Timestamp` + `X-Zeron-Signature` on agent HTTP APIs |
| `AgentHmacSkewSeconds` | `300` | Allowed clock skew for HMAC timestamps |
| `RequireHttpsAgents` | `false` | Reject non-HTTPS agent calls (honors `X-Forwarded-Proto`) |
| `HeartbeatTimeoutSeconds` | `90` | Seconds without heartbeat before agent marked offline |
| `DispatchIntervalMs` | `5000` | Background task dispatch interval |
| `ScheduleIntervalMs` | `15000` | Central cron schedule poll interval |
| `JwtSecret` | (dev secret) | JWT signing key (min 32 chars) |
| `JwtIssuer` | `Zeron.Server` | JWT issuer and audience |
| `JwtExpireMinutes` | `480` | JWT token lifetime |
| `DefaultAdminUsername` | `admin` | Seed admin username (first run only) |
| `DefaultAdminPassword` | `admin123` | Seed admin password (first run only) |
| `AlertEmailEnabled` | `false` | Send email on new alerts |
| `AlertEmailTo` | — | Alert recipient address |
| `SmtpHost` | — | SMTP server hostname |
| `SmtpPort` | `587` | SMTP port |
| `SmtpUser` | — | SMTP username |
| `SmtpPassword` | — | SMTP password |
| `SmtpFrom` | — | From address |
| `SmtpEnableSsl` | `true` | Use TLS for SMTP |

Environment variable override uses double underscore: `Zeron__AgentApiKey`.

### Production Template

Copy `appsettings.Production.template.json` to `appsettings.Production.json` and replace all `CHANGE_ME` placeholders. The template is included in publish output for reference.

```powershell
copy appsettings.Production.template.json appsettings.Production.json
```

Set `ASPNETCORE_ENVIRONMENT=Production` to load production settings.

## Zeron.Demand (`App.config`)

| Key | Description |
|-----|-------------|
| `server_enabled` | `true` to report heartbeat/events to Zeron.Server |
| `server_url` | Base URL of Zeron.Server (e.g. `http://192.168.1.10:5000`) |
| `server_api_key` | Must match server `Zeron:AgentApiKey` (supports `old\|new`) |
| `server_hmac_enabled` | `true` to sign agent HTTP requests with HMAC-SHA256 |
| `zmq_sub_enabled` | Connect to Server command PUB |
| `zmq_sub_addr` | Server PUB address (e.g. `tcp://192.168.1.10:6000`) |
| `zmq_sub_api_key` | API key expected in RemoteCommand payloads |
| `zmq_sub_curve_enabled` | Enable CURVE client on SUB (must match Server `CurveEnabled`) |
| `zmq_sub_curve_server_public_key_file` | Path to Server `.public` key file |
| `zmq_sub_curve_client_secret_file` | Agent client secret (auto-created if missing) |
| `zmq_rep_addr` | Local REQ/REP bind (plaintext; for `Zeron.Client`) |
| `zmq_pub_addr` | Local event PUB bind (plaintext) |

Agent identity is persisted under `Resource/agent.id`. The agent sends heartbeats every 30 seconds when `server_enabled=true`.

### Transport security

| Channel | Protection |
|---------|------------|
| Server PUB ↔ Agent SUB | NetMQ CURVE when enabled on both sides |
| Agent → Server HTTP | Shared key + optional HMAC; prefer HTTPS reverse proxy |
| Local Agent REP/PUB | Plaintext by design (localhost tooling) |

See [deployment.md](./deployment.md) for enablement and key rotation steps.

## Dashboard Roles

| Role | Capabilities |
|------|--------------|
| Admin | Full access, user management, disable/enable agents |
| Operator | Create and manage tasks |
| Viewer | Read-only access to agents, tasks, events, alerts |

Admin users can manage accounts on the Dashboard **Users** page (`/users`).

## Password Policy

| Rule | Behavior |
|------|----------|
| Seeded default admin | `MustChangePassword=true` — redirected to `/account/change-password` after login |
| Existing default password | If admin password still matches `DefaultAdminPassword`, it is marked for forced change on startup |
| Admin-created users | Must change password on first login |
| Admin password reset | Target user must change password on next login |
| Self-service | Any logged-in user can open **Change Password** in the sidebar |

Endpoints:

- `POST /account/change-password` — form post (Dashboard)
- `POST /api/auth/change-password` — JSON API (`currentPassword`, `newPassword`)

While `MustChangePassword` is true, other Dashboard pages and APIs return redirect / `403` until the password is updated.

## Health Endpoints

| Endpoint | Auth | Description |
|----------|------|-------------|
| `GET /health` | Anonymous | Liveness probe — process is running |
| `GET /ready` | Anonymous | Readiness probe — SQLite database is reachable |

Use `/ready` for load balancer / process manager health checks.

## Dashboard Summary

| Endpoint | Auth | Description |
|----------|------|-------------|
| `GET /api/dashboard/summary` | Viewer+ | Aggregated online/offline agents, stale connections, active tasks, open alerts, recent lists, transport security |

The Dashboard home page (`/`) uses this summary and refreshes every 15 seconds (plus SignalR updates).

### Transport security panel

Summary field `security` reports server-side posture:

| Field | Meaning |
|-------|---------|
| `curveEnabled` | `Zeron:CurveEnabled` |
| `curvePublicKeyPresent` | Public key file exists at `CurvePublicKeyPath` |
| `agentHmacRequired` | `Zeron:AgentHmacRequired` |
| `requireHttpsAgents` | `Zeron:RequireHttpsAgents` |
| `overallStatus` | `hardened` (CURVE+HMAC), `partial` (one of them), or `insecure` |
| `recommendations` | Operator tips when controls are off / misconfigured |

Agents must still enable matching `zmq_sub_curve_*` and `server_hmac_enabled` — the panel reflects **server** configuration only.

## Task Schedules

Central cron schedules live on `Zeron.Server` (not agent `scheduler-tasks.json`).

| Endpoint | Auth | Description |
|----------|------|-------------|
| `GET /api/schedules` | Viewer+ | List schedules |
| `POST /api/schedules` | Operator+ | Create schedule |
| `PUT /api/schedules/{id}` | Operator+ | Update schedule |
| `POST /api/schedules/{id}/enable` | Operator+ | Enable |
| `POST /api/schedules/{id}/disable` | Operator+ | Disable |
| `POST /api/schedules/{id}/run` | Operator+ | Trigger immediately |
| `DELETE /api/schedules/{id}` | Admin | Delete |

Cron uses 5-field NCrontab expressions evaluated in **server local time**. When due, `TaskScheduleWorker` creates a normal `TaskEntity` via `TaskDispatcherServer` (same path as one-shot tasks).

Dashboard pages: `/schedules`, `/schedules/create`, `/schedules/{id}`.

## ManagedPackage Central Deploy

Central package deploy reuses the task dispatch path (`TargetApi=ManagedPackage`).

| Endpoint | Auth | Description |
|----------|------|-------------|
| `POST /api/packages/deploy` | Operator+ | Create install/uninstall deploy task |
| `GET /api/packages/deploys` | Viewer+ | Recent ManagedPackage tasks |
| `GET /api/packages/install-events` | Viewer+ | Recent `install.*` events |

Command format sent to agents:

```text
install <packageName> [extraArgs]
uninstall <packageName> [extraArgs]
```

Package names must exist in each agent's local SQLite `managed_packages` catalog (`status=1`).

Package deploy assignment lifecycle:

1. `dispatched` → agent receives RemoteCommand  
2. `running` → package accepted into agent install queue (`queued: true`)  
3. `completed` / `failed` → `install.completed` / `install.failed` (payload includes `assignmentId`)

Related events: `install.started` / `install.uninstall` / `install.completed` / `install.failed`.

Dashboard pages: `/packages`, `/packages/deploy`. Events shortcut: `/events?topic=install.`.
