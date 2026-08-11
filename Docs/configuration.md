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
| `CatalogSyncStaleMinutes` | `15` | Online agents whose last catalog sync is older than this are stale on Sync Health |
| `PublishAgentHeartbeatEvents` | `false` | Emit `agent.heartbeat` on the in-process event bus (noisy; see [Event Bus](./event-bus.md)) |
| `DispatchIntervalMs` | `5000` | Background task dispatch interval |
| `ScheduleIntervalMs` | `15000` | Central cron schedule poll interval |
| `JwtSecret` | (dev secret) | JWT signing key (min 32 chars) |
| `JwtIssuer` | `Zeron.Server` | JWT issuer and audience |
| `JwtExpireMinutes` | `480` | JWT token lifetime |
| `DefaultAdminUsername` | `admin` | Seed admin username (first run only) |
| `DefaultAdminPassword` | `admin123` | Seed admin password (first run only) |
| `AlertEmailEnabled` | `false` | Send email on new alerts |
| `AlertEmailTo` | — | Alert recipient address |
| `InstallResultNotifyEnabled` | `true` | Create My Devices tips when self-service install completes/fails |
| `InstallResultEmailEnabled` | `false` | Email bound users (with `Users.Email`) on self-service install results (uses `Smtp*`) |
| `SmtpHost` | — | SMTP server hostname |
| `SmtpPort` | `587` | SMTP port |
| `SmtpUser` | — | SMTP username |
| `SmtpPassword` | — | SMTP password |
| `SmtpFrom` | — | From address |
| `SmtpEnableSsl` | `true` | Use TLS for SMTP |
| `EncryptionSaltKey` | (legacy default) | AES salt for NetMQ API-key obfuscation; must match agents |
| `EncryptionIvKey` | (legacy default) | AES IV source for NetMQ API-key obfuscation; must match agents |
| `WindowsServiceName` | `Zeron.Server` | SCM service name when hosted with `UseWindowsService` |

Environment variable override uses double underscore: `Zeron__AgentApiKey`.

Crypto env overrides (applied after config): `ZERON_CRYPT_SALT`, `ZERON_CRYPT_IV`.

### Production Template

Copy `appsettings.Production.template.json` to `appsettings.Production.json` and replace all `CHANGE_ME` placeholders. The template enables CURVE, HMAC, HTTPS URLs, shared encryption keys, and SMTP placeholders. It is included in publish output for reference.

```powershell
copy appsettings.Production.template.json appsettings.Production.json
```

Set `ASPNETCORE_ENVIRONMENT=Production` to load production settings. Align agent `App.config` with the same template values using `Zeron.Demand/App.Sample.config`.

## Zeron.Demand (`App.config`)

Start from `App.Sample.config` (production-shaped) or the published sample next to the agent binary.

| Key | Description |
|-----|-------------|
| `server_enabled` | `true` to report heartbeat/events to Zeron.Server |
| `server_url` | Base URL of Zeron.Server (e.g. `https://192.168.1.10:5000`) |
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
| `encryption_salt_key` | Must match Server `EncryptionSaltKey` (or `ZERON_CRYPT_SALT`) |
| `encryption_iv_key` | Must match Server `EncryptionIvKey` (or `ZERON_CRYPT_IV`) |
| `script_powershell_enabled` | Enable built-in PowerShell script engine (`true` default) |
| `script_powershell_exe` | PowerShell executable (`powershell.exe` default) |
| `script_default_timeout_ms` | Default script timeout in ms (`300000`) |
| `script_engine_{id}_enabled` | Register external process engine `{id}` (`true` to enable) |
| `script_engine_{id}_exe` | Executable for that engine |
| `script_engine_{id}_args` | Args template (`{scriptPath}`, `{arguments}`, `{script}`) |
| `script_engine_{id}_platforms` | e.g. `windows,linux,macos` |
| `script_engine_{id}_inline_mode` | `stdin` / `tempfile` / `none` |
| `script_engine_{id}_display` | Optional display name |
| `script_event_listener_enabled` | Run ScriptEventBridge NDJSON listener (`false` default) |
| `script_event_listener_exe` | Listener executable |
| `script_event_listener_args` | Listener arguments |
| `script_event_listener_restart_ms` | Restart backoff after listener exit (`3000`) |
| `mail_enabled` | `true` to enable agent-side SMTP (`MailerServer`) |
| `mail_host` / `mail_port` | SMTP server |
| `mail_user_login` / `mail_user_password` | SMTP credentials (optional if relay allows anonymous) |
| `mail_sender_name` / `mail_sender_address` | From identity |
| `mail_recipients_administrator` | Comma/space-separated admin recipients |
| `mail_enable_ssl` | Use TLS for SMTP (`true` default) |

Script engines: see [Script Host](./script-host.md) for engine IDs, Pipeline `powershell` vs `script`, and capability reporting.

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

## ManagedPackage Catalog and Central Deploy

The Server catalog is the central source of ManagedPackage definitions. Demand agents periodically pull `/api/packages/catalog/sync` and upsert rows with `source=server`. Rows marked `source=local` on Demand are never overwritten (standalone Demand or local overrides).

| Endpoint | Auth | Description |
|----------|------|-------------|
| `GET /api/packages/catalog` | Viewer+ | List Server catalog |
| `POST /api/packages/catalog` | Operator+ | Create catalog package |
| `PUT /api/packages/catalog/{id}` | Operator+ | Update catalog package |
| `DELETE /api/packages/catalog/{id}` | Operator+ | Delete catalog package |
| `GET /api/packages/catalog/{id}/versions` | Viewer+ | Package version history |
| `GET /api/packages/catalog/{id}/versions/{n}` | Viewer+ | Single version snapshot |
| `POST /api/packages/catalog/{id}/rollback` | Operator+ | Restore version `n` onto live package and push sync |
| `GET /api/packages/catalog/sync` | Agent API key | Full catalog snapshot for Demand |
| `GET /api/packages/catalog/sync-health` | Viewer+ | Catalog sync health summary per agent |
| `POST /api/packages/catalog/sync-push` | Operator+ | Push `ManagedPackage sync` to unhealthy / selected / all online agents |
| `POST /api/packages/deploy` | Operator+ | Create install/uninstall deploy task (name must exist and be enabled in Server catalog) |
| `GET /api/packages/deploys` | Viewer+ | Recent ManagedPackage tasks |
| `GET /api/packages/install-events` | Viewer+ | Recent `install.*` events |

Demand App.config keys:

| Key | Description |
|-----|-------------|
| `mp_db_source_file` | Local SQLite path (created if missing) |
| `mp_repo_temp_path` | Download temp folder |
| `mp_catalog_sync_enabled` | Sync from Server when reporter is enabled (default `true`) |
| `mp_catalog_sync_interval_ms` | Sync interval (default `300000`) |

Command format sent to agents:

```text
install <packageName> [extraArgs]
uninstall <packageName> [extraArgs]
```

Package deploy assignment lifecycle:

1. `dispatched` → agent receives RemoteCommand  
2. `running` → package accepted into agent install queue (`queued: true`)  
3. `completed` / `failed` → `install.completed` / `install.failed` (payload includes `assignmentId`)

Related events: `install.started` / `install.uninstall` / `install.completed` / `install.failed`.

Dashboard pages: `/packages`, `/packages/catalog`, `/packages/sync-health`, `/packages/deploy`. Events shortcut: `/events?topic=install.`.

**Sync Health** (`/packages/sync-health`): classifies each agent as `healthy` / `stale` / `never` / `failed` / `offline` using `LastCatalogSyncAt` and recent failed `package.catalog.sync` audit rows. Operators can push sync to unhealthy online agents, all online agents, or a single agent. Stale window: `CatalogSyncStaleMinutes`.

## Device Owner Self-Service

`DeviceOwner` accounts can log into the Dashboard and view only Demand agents bound to their user (Admin manages bindings).

| Endpoint | Auth | Description |
|----------|------|-------------|
| `GET /api/user-agent-bindings` | Admin | List bindings |
| `POST /api/user-agent-bindings` | Admin | Bind user ↔ AgentKey |
| `DELETE /api/user-agent-bindings/{id}` | Admin | Remove binding |
| `GET /api/my/devices` | DeviceOwner or staff | Bound agent status |
| `GET /api/my/devices/{agentKey}` | DeviceOwner or staff | Single bound agent |
| `GET /api/my/devices/{agentKey}/install-events` | DeviceOwner or staff | Install events for bound agent |
| `POST /api/my/devices/{agentKey}/deploy` | DeviceOwner or staff | Self-service install/uninstall (bound agent + Server catalog only) |
| `GET /api/my/notifications` | DeviceOwner or staff | Install-result tips (`unreadOnly`, `limit`) |
| `POST /api/my/notifications/{id}/read` | DeviceOwner or staff | Dismiss one tip |
| `POST /api/my/notifications/read-all` | DeviceOwner or staff | Dismiss all tips |

Demand ManagedPackage local commands (Client / RemoteCommand):

| Command | Description |
|---------|-------------|
| `list` | List local catalog (`name`, `source`, `enabled`) |
| `sync` | Pull Server catalog now |
| `override <name>` | Mark row `source=local` (sync will not overwrite) |
| `clear-override <name>` | Delete local-override row so next sync can recreate Server version |
| `status` / `install` / `uninstall` | Existing install queue commands |

Catalog packages may include optional `Sha256x86` / `Sha256x64`; Demand verifies the digest after download. Catalog create/update/delete/rollback pushes `ManagedPackage sync` to online agents. Heartbeat reports `LastCatalogSyncAt` for My Devices / agent status.

**Catalog versions:** each successful create/update/rollback appends a `ManagedPackageVersions` snapshot (`create` / `update` / `rollback`), including `ScriptEngine` and before/after scripts. Operators can Restore a prior version from Catalog **History**; restore writes a new version row (does not rewrite history) and re-pushes sync. Deleting a package cascades its version rows away.

**ScriptEngine:** catalog field (default `powershell`) selects the Script Host engine for install/uninstall hooks on Demand. Deploy to explicit `AgentIds` fails if the agent does not report that engine as available. See [Script Host](./script-host.md).

**Install result notifications:** when a self-service deploy (`Task.Name` starts with `self-`) finishes with `install.completed` / `install.failed`, Server notifies every active user bound to that agent. Dashboard tips appear on `/my-devices` (`InstallResultNotifyEnabled`). Optional per-user email uses `Users.Email` + SMTP (`InstallResultEmailEnabled`). Staff package deploys are not notified this way.

Dashboard pages: `/my-devices`, `/device-bindings` (Admin).

## Audit / 操作紀錄

Server stores attributed operations in `AuditLogs` (Dashboard **Audit** `/audit`, API `GET /api/audit`).

| Action | Source | When |
|--------|--------|------|
| `catalog.create` / `update` / `delete` / `rollback` | server | Catalog CRUD and restore |
| `package.deploy` | server | Staff package deploy |
| `package.self_deploy` | server | DeviceOwner self-service deploy |
| `binding.create` / `binding.delete` | server | Device bindings |
| `package.override` / `package.clear-override` / `package.catalog.sync` | agent | Demand ManagedPackage commands (via events) |
| `catalog.sync.push` | server | Operator push sync from Sync Health |

Query filters: `action`, `actor`, `target`, `source`, `limit`.
