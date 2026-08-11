# Script Host

Zeron agents run scripts through a pluggable **Script Host** (`ScriptHostServer` + `IScriptEngine`). This keeps PowerShell as the built-in engine and leaves a clear extension point for AutoIt / ThinBasic / Vyrn later.

## Engine IDs

| Id | Platforms (current) | Notes |
|----|---------------------|--------|
| `powershell` | `windows` | Built-in (`powershell.exe`) |
| `vyrn` | `windows` `linux` `mac` | Planned |
| `autoit` | `windows` | Planned |
| `thinbasic` | `windows` | Planned |

Unknown engine IDs fail with a clear error (not a crash).

## Pipeline steps

Legacy:

```json
{ "type": "powershell", "script": "Write-Output 'ok'" }
```

Generic (preferred for new work):

```json
{ "type": "script", "engine": "powershell", "script": "Write-Output 'ok'" }
```

If `engine` is omitted on a `script` step, it defaults to `powershell`.

## ManagedPackage install hooks

Catalog packages carry a `ScriptEngine` field (default `powershell`). Demand runs `ScriptInstallBefore` / `ScriptInstallAfter` (and uninstall variants) through `ScriptHostServer` using that engine. The value is included in catalog version snapshots, so **rollback restores engine + scripts together**, then sync pushes the definition to agents.

Package Deploy to specific `AgentIds` rejects targets that do not report the engine as available in heartbeat `supportedEngines` (empty capability JSON still allows `powershell` for older agents). Sync Health remains about catalog freshness, not engine availability.

## Agent capability reporting

- Heartbeat field: `supportedEngines` (`ScriptEngineInfoType[]`)
- Local `HealthCheck` field: `scriptEngines`
- Server stores JSON on `Agents.SupportedEnginesJson` and shows it on the Agent detail page

## Configuration (App.config)

| Key | Default | Description |
|-----|---------|-------------|
| `script_powershell_enabled` | `true` | Register/enable PowerShell engine |
| `script_powershell_exe` | `powershell.exe` | Executable name or full path |
| `script_default_timeout_ms` | `300000` | Default timeout for script runs |

## Adding an engine

1. Implement `IScriptEngine` in `Zeron/ZCore/Utils/Engines/`
2. Register it from `ScriptHostBootstrapServer.Initialize` (or config-driven registration)
3. Report via `ScriptHostServer.ListEngines()` automatically once registered
