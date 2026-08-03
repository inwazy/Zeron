# Zeron

**Fast, Lightweight Windows Remote Automation for Internal Networks.**  

Driven by NetMQ (ZeroMQ), Zeron empowers SMB IT teams and developers to control systems, deploy software, and build task pipelines via clean JSON-RPC APIs.

![GitHub](https://img.shields.io/github/license/inwazy/Zeron)
![.NET](https://img.shields.io/badge/.NET-10.0-windows)
![Travis (.com)](https://img.shields.io/travis/com/jiowcl/Zeron)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/6bf8bdd0b9634cf3b8c50079e6bbbbfd)](https://www.codacy.com/gh/jiowcl/Zeron/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=jiowcl/Zeron&amp;utm_campaign=Badge_Grade)

![Screenshot](./Zeron.Server/Screenshot/Dashboard.png?raw=true)

## Why Zeron?  

* **⚡ Ultra-Lightweight**: Built on NetMQ (ZeroMQ) for high-performance, low-latency messaging.
* **🛡️ Private & Secure**: Designed specifically for internal networks and SMB environments — no cloud required.
* **🔌 Developer-Friendly**: Exposes standard JSON-RPC APIs for full control over deployments, scheduling, and pipelines.
* **⚙️ Always-On Reliable Agent**: Runs seamless background management via `Zeron.Demand` Windows Service.

## Projects

| Project | Role |
|---------|------|
| `Zeron` | Shared library (servers, attributes, utilities) |
| `Zeron.Demand` | Windows Service agent |
| `Zeron.Client` | Interactive test console |
| `Zeron.Server` | Central management server |
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
- **Zeron.Server** — Central HTTP API + NetMQ command PUB for multi-agent management
- **Dashboard** — Blazor Server UI with JWT/Cookie auth and role-based access
- **Offline Alerts** — Automatic `agent.offline` alerts with optional email notification
- **User Management** — Admin CRUD for Admin / Operator / Viewer accounts
- **Password Policy** — Forced change on first login + self-service change password
- **Dashboard Summary** — Home overview of agents, tasks, alerts, and events with live refresh
- **Task Schedules** — Central cron schedules that dispatch remote APIs to agents
- **Package Deploy** — Central ManagedPackage install/uninstall to selected agents
- **Health Probes** — `/health` and `/ready` for deployment monitoring

## How to Build

Building requires [Visual Studio 2026 Community](https://visualstudio.microsoft.com/vs/community/) and test under Windows 11.

```powershell
dotnet build Zeron.sln -c Release
dotnet test Zeron.sln -c Release
```

CI on GitHub Actions builds, tests, and publishes deploy zips (`zeron-server-win-x64`, `zeron-agent-win-x64`). See [Docs/deployment.md](./Docs/deployment.md).

Run the agent:

```powershell
dotnet run --project Zeron.Demand
```

Run the test client:

```powershell
dotnet run --project Zeron.Client
```

Run the central server:

```powershell
dotnet run --project Zeron.Server
```

Default Dashboard: `http://localhost:5000` (admin / admin123 in Development).

## Documentation

Detailed guides are in the [`Docs/`](./Docs/) directory:

- [Deployment Guide](./Docs/deployment.md) — production setup for server and agents
- [Configuration Reference](./Docs/configuration.md) — server and agent settings
- [Agent Connection Guide](./Docs/agent-connection.md) — heartbeat, diagnostics, troubleshooting
- [Testing Guide](./Docs/testing.md) — unit tests, E2E tests, EF migrations

Production settings: copy `Zeron.Server/appsettings.Production.template.json` to `appsettings.Production.json` and set secrets before deploy.

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
