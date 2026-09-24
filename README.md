# LamuFlix

[![CI](https://github.com/lamuniercosta/lamuflix/actions/workflows/ci.yml/badge.svg)](https://github.com/lamuniercosta/lamuflix/actions/workflows/ci.yml)

LamuFlix is a media management and playback application modernization project.

## Modernization to .NET 10

### DEV-14: Web, Data, and Test Upgrade
The Web and Data projects—as well as the test suite (`LamuFlix.Test`)—have been upgraded from .NET Core 2.1 to **.NET 10 (`net10.0`)**.

Notable changes:
- Target frameworks upgraded to `net10.0`.
- Removed explicit `Microsoft.AspNetCore.App` metapackage reference (now implicit in `Microsoft.NET.Sdk.Web`).
- Migrated from `IHostingEnvironment` to `IWebHostEnvironment`.
- Upgraded `Pomelo.EntityFrameworkCore.MySql` to `9.0.0` with explicit server versioning (`MySqlServerVersion`).
- Replaced legacy `UseMvc` with modern endpoint routing (`UseRouting`, `UseAuthorization`, `UseEndpoints`) and `AddControllersWithViews()`.
- Modernized MSTest and EF Core In-Memory test dependencies.

### DEV-15: Worker BackgroundService
The legacy .NET Framework 4.6.1 WinForms worker in `LamuFlix.Work` has been replaced with a modern headless **.NET 10 `BackgroundService`** (`QueueWorker`):
- Deleted legacy WinForms UI artifacts (`Form1.cs`, `Form1.Designer.cs`, `Form1.resx`, `App.config`, `Properties/Resources.*`, `Properties/Settings.*`, `packages.config`).
- Upgraded `LamuFlix.Worker.csproj` to SDK-style project (`Microsoft.NET.Sdk.Worker`) targeting `net10.0` with nullable reference types enabled.
- Replaced the WinForms loop with an asynchronous hosted service consuming from RabbitMQ (`task_queue`) via `EventingBasicConsumer` with graceful shutdown support.
- Configured generic hosting with `Host.CreateDefaultBuilder` and `appsettings.json`.
- Added unit tests for `QueueWorker` in `LamuFlix.Test`.
- The entire solution (`LamuFlix.sln`) now compiles with zero errors on .NET 10.

---

## Verification & Building
To build the complete solution and run tests:
```bash
dotnet build LamuFlix.sln
dotnet test
```
