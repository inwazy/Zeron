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

Scripts **cannot** block Install, RemoteCommand, or Catalog Sync. .NET gate intercept is Phase 3.

## Sample listener

See [`Zeron.Demand/Resource/script-event-listener.sample.ps1`](../Zeron.Demand/Resource/script-event-listener.sample.ps1).
