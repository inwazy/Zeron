# Script Host

Zeron agents run scripts through a pluggable **Script Host** (`ScriptHostServer` + `IScriptEngine`). PowerShell is built in; additional engines are registered from App.config as external processes (no in-tree language runtimes).

## Engine IDs

| Id | Platforms (current) | Notes |
|----|---------------------|--------|
| `powershell` | `windows` | Built-in (`powershell.exe`) |
| *(user-defined)* | from config | `script_engine_{id}_*` → `ExternalProcessScriptEngine` |

Unknown engine IDs fail with a clear error (not a crash). Reserved: do not register external id `powershell` (built-in wins; config key is ignored).

## Pipeline steps

Legacy:

```json
{ "type": "powershell", "script": "Write-Output 'ok'" }
```

Generic (preferred for new work):

```json
{ "type": "script", "engine": "powershell", "script": "Write-Output 'ok'" }
```

If `engine` is omitted on a `script` step, it defaults to `powershell`. Any registered external id works the same way (`"engine": "pwsh"`).

## ManagedPackage install hooks

Catalog packages carry a `ScriptEngine` field (default `powershell`). Demand runs `ScriptInstallBefore` / `ScriptInstallAfter` (and uninstall variants) through `ScriptHostServer` using that engine. The value is included in catalog version snapshots, so **rollback restores engine + scripts together**, then sync pushes the definition to agents.

Package Deploy to specific `AgentIds` rejects targets that do not report the engine as available in heartbeat `supportedEngines` (empty capability JSON still allows `powershell` for older agents). Sync Health remains about catalog freshness, not engine availability.

Install (and RemoteCommand / Server dispatch) can be **Pause / Cancel** by in-process .NET plugins only — scripts cannot intercept. See [Event Bus](./event-bus.md).

## External process engines

Register user tools without compiling against Zeron:

```xml
<add key="script_engine_pwsh_enabled" value="true" />
<add key="script_engine_pwsh_exe" value="pwsh" />
<add key="script_engine_pwsh_args" value="-NoProfile -File {scriptPath} {arguments}" />
<add key="script_engine_pwsh_platforms" value="windows,linux,macos" />
<add key="script_engine_pwsh_inline_mode" value="tempfile" />
<add key="script_engine_pwsh_display" value="PowerShell 7" />
```

| Key suffix | Description |
|------------|-------------|
| `_enabled` | `true` to register |
| `_exe` | Executable name or full path |
| `_args` | Argument template: `{scriptPath}`, `{arguments}`, `{script}` |
| `_platforms` | Comma-separated (`windows`, `linux`, `macos`) |
| `_inline_mode` | `stdin` (default), `tempfile`, or `none` |
| `_display` | Optional display name |

**Inline modes**

- `stdin` — write inline `Script` to process stdin  
- `tempfile` — write inline `Script` to a temp file and expand `{scriptPath}`  
- `none` — require `ScriptPath` (or empty script = success)

**Optional result JSON:** if the **last non-empty stdout line** is JSON like `{"success":false,"exitCode":9,"message":"..."}`, it overrides success / exit code / error message (stdout/stderr are still captured).

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
| `script_engine_{id}_*` | — | External process engines (see above) |

## Adding an engine

**Preferred:** App.config `script_engine_*` keys (no code change).

**Advanced:** implement `IScriptEngine` and register from `ScriptHostBootstrapServer.Initialize`.
