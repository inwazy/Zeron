# Zeron

A lightweight Windows Agent Platform Suitable for SMB/internal network IT/developer-built Automation Tools.

Zeron is an open source Windows remote automation platform based on NetMQ (ZeroMQ). It runs as a Windows Service agent (`Zeron.Demand`) and exposes JSON RPC APIs for software deployment, system control, scheduling, and task pipelines.

![GitHub](https://img.shields.io/github/license/inwazy/Zeron)
![.NET](https://img.shields.io/badge/.NET-10.0-windows)
![Travis (.com)](https://img.shields.io/travis/com/jiowcl/Zeron)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/6bf8bdd0b9634cf3b8c50079e6bbbbfd)](https://www.codacy.com/gh/jiowcl/Zeron/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=jiowcl/Zeron&amp;utm_campaign=Badge_Grade)

## Projects

| Project | Role |
|---------|------|
| `Zeron` | Shared library (servers, attributes, utilities) |
| `Zeron.Demand` | Windows Service agent |
| `Zeron.Client` | Interactive test console |
| `ZeronTests` | Unit and integration tests |

## Features

- **NetMQ REQ/REP** — JSON RPC on port `5589`
- **NetMQ PUB** — Event stream on port `5588` (install, task, filesystem, etc.)
- **NetMQ SUB** — Remote push commands via `RemoteCommand` handler
- **ManagedPackage** — SQLite-driven software install/uninstall
- **Scheduler** — NCrontab cron tasks via `Resource/scheduler-tasks.json`
- **Task Pipeline** — JSON-defined multi-step workflows
- **Agent Identity** — Persistent `AgentId` + `HealthCheck` API
- **API Scopes** — RBAC via `zmq_rep_api_scopes` config
- **Audit Log** — SQLite audit trail at `Resource/audit.db`

## How to Build

Building requires [Visual Studio 2026 Community](https://visualstudio.microsoft.com/vs/community/) and test under Windows 11.

```powershell
dotnet build Zeron.sln -c Release
dotnet test Zeron.sln -c Release
```

Run the agent:

```powershell
dotnet run --project Zeron.Demand
```

Run the test client:

```powershell
dotnet run --project Zeron.Client
```

## Task Pipeline Example

`Resource/scheduler-tasks.json`:

```json
{
  "tasks": [
    {
      "name": "daily-serverinfo",
      "cron": "0 8 * * *",
      "enabled": true,
      "steps": [
        { "type": "api", "apiName": "ServerInfo", "command": "" }
      ]
    }
  ]
}
```

## License

Copyright (c) 2017-2026 Ji-Feng Tsai.  
Code released under the MIT license.

## Donation

If this application help you reduce time to coding, you can give me a cup of coffee :)

[![paypal](https://www.paypalobjects.com/en_US/TW/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=3RNMD6Q3B495N&source=url)

[Paypal Me](https://paypal.me/jiowcl?locale.x=zh_TW)
