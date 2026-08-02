# Configuration Reference

## Zeron.Server (`appsettings.json` / `Zeron` section)

| Key | Default | Description |
|-----|---------|-------------|
| `DatabasePath` | `Data/zeron-server.db` | SQLite database file path |
| `CommandPubAddr` | `tcp://*:6000` | NetMQ PUB bind address for agent commands |
| `AgentApiKey` | (dev key) | Expected value of `X-Zeron-Agent-Key` header from agents |
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
| `server_api_key` | Must match server `Zeron:AgentApiKey` |
| `zmq_rep_port` | Local REQ/REP API port (default `5589`) |
| `zmq_pub_port` | Local event PUB port (default `5588`) |

Agent identity is persisted under `Resource/agent-id.txt`. The agent sends heartbeats every 30 seconds when `server_enabled=true`.

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
| `GET /api/dashboard/summary` | Viewer+ | Aggregated online/offline agents, stale connections, active tasks, open alerts, recent lists |

The Dashboard home page (`/`) uses this summary and refreshes every 15 seconds (plus SignalR updates).

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
