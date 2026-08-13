# SampleAgentGatePlugin

Demand-side `.NET` gate sample: observes `install.*` / `gate.cancelled`, and can **Pause → auto-Resume** or **Cancel** `gate.install`.

Scripts still cannot intercept. This DLL is trusted in-process code.

## Build and drop in

```powershell
dotnet build Samples/SampleAgentGatePlugin/SampleAgentGatePlugin.csproj -c Release
```

Copy **only** `SampleAgentGatePlugin.dll` into the agent `plugins/` folder (`script_plugins_dir`, default next to `Zeron.Demand.exe`).

**Do not** name the DLL `Zeron.*.dll` — the loader skips `Zeron.*` / `System.*` / `Microsoft.*`.

Use a framework-dependent agent build (Debug/`dotnet run`). Single-file publish may fail to load plugins.

```xml
<add key="script_plugins_dir" value="plugins" />
<add key="gate_pause_timeout_ms" value="300000" />
```

Restart Demand. Log line: `SampleAgentGatePlugin ready. mode=...`

## Modes

Default is **`proceed`** (observe only; install is not blocked).

| Mode | How | Effect on `gate.install` |
|------|-----|--------------------------|
| `proceed` | default | No intercept |
| `pause-resume` | Pause, then Resume after delay | Install continues after delay |
| `cancel` | Cancel immediately | Install does not start |

**Environment (overrides files):**

```powershell
$env:ZERON_SAMPLE_GATE_MODE = "pause-resume"   # or cancel / proceed
$env:ZERON_SAMPLE_GATE_DELAY_MS = "2000"
$env:ZERON_SAMPLE_GATE_PACKAGE = "ccleaner"    # optional; only this package
```

**Files next to the DLL** (used when env is unset):

| File | Example |
|------|---------|
| `SampleAgentGatePlugin.mode` | `pause-resume` |
| `SampleAgentGatePlugin.delay-ms` | `2000` |
| `SampleAgentGatePlugin.package` | `ccleaner` |

Copy `SampleAgentGatePlugin.mode.sample` → `SampleAgentGatePlugin.mode`. Change mode between installs without rebuilding; restart is not required (re-read on each gate).

`gate_pause_timeout_ms` must be greater than `ZERON_SAMPLE_GATE_DELAY_MS` or Pause times out and becomes Cancel.
