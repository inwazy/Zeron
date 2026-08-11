# Sample ScriptEventBridge listener (observe only).
# Wire via App.config:
#   script_event_listener_enabled=true
#   script_event_listener_exe=powershell.exe
#   script_event_listener_args=-NoProfile -ExecutionPolicy Bypass -File Resource/script-event-listener.sample.ps1

$ErrorActionPreference = 'Stop'

while ($null -ne ($line = [Console]::In.ReadLine())) {
    if ([string]::IsNullOrWhiteSpace($line)) {
        continue
    }

    try {
        $msg = $line | ConvertFrom-Json
    }
    catch {
        continue
    }

    if ($msg.type -ne 'event') {
        continue
    }

    # Example: acknowledge catalog sync observations
    if ($msg.topic -eq 'package.catalog.sync') {
        Write-Output (@{ type = 'ack'; correlationId = $msg.correlationId } | ConvertTo-Json -Compress)
    }

    # Never send cancel / pause_gate / pause_sync — Agent will reject them.
}
