# Event Bus

Zeron exposes an in-process **event bus** (`IZeronEventBus` / `ZeronEventBus.Current`) on both Server and Demand. Observers can subscribe without changing the HTTP / NetMQ control plane.

## Topics

### Server

| Topic | When |
|-------|------|
| `agent.connected` | First register or offline → online |
| `agent.heartbeat` | Each heartbeat (**off** unless `Zeron:PublishAgentHeartbeatEvents=true`) |
| `agent.offline` | Heartbeat monitor marks agent offline |
| `task.dispatched` | RemoteCommand PUB sent |
| `event.ingested` | Agent HTTP event stored |
| `catalog.rolled_back` | Catalog Restore succeeded |
| `catalog.sync_requested` | Server pushed ManagedPackage `sync` to agents |

### Agent (Demand)

Many topics already flow through `InstallEventPublisher` and are **dual-written** to the bus (including `install.*`, `task.*`, `package.override`, `remotecommand.executed`).

| Topic | When |
|-------|------|
| `package.catalog.sync` | After `ManagedPackageServer.SyncCatalogAsync` (command or timer) |
| `command.received` | RemoteCommand before invoke |
| `command.completed` | RemoteCommand after invoke (also `remotecommand.executed`) |
| `script.started` / `script.completed` / `script.failed` | `ScriptHostServer.Execute` (bus-only; not forwarded as HTTP events) |

## .NET subscription

```csharp
using IDisposable sub = ZeronEventBus.Current.Subscribe("package.catalog.sync", evt =>
{
    // evt.Topic, evt.PayloadJson, evt.CorrelationId
});

// Prefix: "install.*"   All: "*" or null
```

Server DI also registers `IZeronEventBus` → `ZeronEventBus.Current`.

## Script event listener (observe only)

Optional long-running process receives NDJSON events on stdin:

```xml
<add key="script_event_listener_enabled" value="true" />
<add key="script_event_listener_exe" value="pwsh" />
<add key="script_event_listener_args" value="-File Resource/script-event-listener.ps1" />
<add key="script_event_listener_restart_ms" value="3000" />
```

**Agent → script (stdin line):**

```json
{"type":"event","topic":"package.catalog.sync","correlationId":null,"payload":{...}}
```

**Script → Agent (stdout line):**

| type | Effect |
|------|--------|
| `ack` | Ignored (optional) |
| `pause_self` | Queue events; do not deliver until resume |
| `resume_self` | Flush queued events |
| `cancel` / `pause_gate` / `pause_sync` / … | Rejected with `{"type":"error","code":"not_allowed"}` |

Scripts **cannot** block Install, RemoteCommand, or Catalog Sync.

## .NET Gate (intercept)

Only in-process .NET handlers (`IGateHandler` / `IGateController`) can Pause / Resume / Cancel. Scripts sending `cancel` / `pause_gate` are rejected.

| Topic | Where |
|-------|--------|
| `gate.command` | Agent: RemoteCommand before invoke |
| `gate.install` | Agent: Install before execute |
| `gate.dispatch` | Server: task dispatch before PUB |
| `gate.cancelled` | Emitted when work is cancelled (including pause timeout) |

```csharp
ZeronGateServer.Current.Register(new MyHandler()); // Handle() may set Decision = Pause|Cancel
ZeronGateServer.Current.Resume(correlationId);
ZeronGateServer.Current.Cancel(correlationId, "reason");
```

- `Pause` waits until Resume, Cancel, or `gate_pause_timeout_ms` (Agent default 300000; Server `GatePauseTimeoutMs` default 2000). Timeout → Cancel.
- Demand loads `IZeronAgentPlugin` DLLs from `script_plugins_dir` (default `plugins/`). **Skipped names:** `Zeron.*`, `System.*`, `Microsoft.*`, `netstandard*`.
- Server plugins: register in-process via `IGateController` (DI: `IGateController` → `ZeronGateServer.Current`). `agent.connected` remains post-hook only (no gate).
- Treat `plugins/` as trusted code (same process as the agent).

## Sample listener

See [`Zeron.Demand/Resource/script-event-listener.sample.ps1`](../Zeron.Demand/Resource/script-event-listener.sample.ps1).

## Sample .NET Gate plugin

[`Samples/SampleAgentGatePlugin`](../Samples/SampleAgentGatePlugin/README.md) — observe `install.*`, intercept `gate.install`.

```powershell
dotnet build Samples/SampleAgentGatePlugin/SampleAgentGatePlugin.csproj -c Release
# copy SampleAgentGatePlugin.dll → Zeron.Demand plugins/
$env:ZERON_SAMPLE_GATE_MODE = "pause-resume"   # proceed | pause-resume | cancel
```

Default mode is `proceed` (log only). `pause-resume` Pause then auto-Resume after `ZERON_SAMPLE_GATE_DELAY_MS` (2000). `cancel` aborts install.
