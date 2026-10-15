# DEV-23 Conclusions (Patron)

## R1 - dotnet test infra failure (116 Npgsql refused to 127.0.0.1:5432)

Verdict: do not touch `ApiHostFactory.cs` or the machine user-secrets in DEV-23; re-run the test gate with a process-scoped env override; file a follow-up ticket for the test-isolation fix.

- Scope: DEV-23 is README-only (ticket Scope/AC); `tests/LamuFlix.IntegrationTests/ApiHostFactory.cs` is an unnamed tracked file and no AC or diff-caused gate forces the edit (Patron rules, care item 6). Cog: 0 failures point at the diff.
- Secrets: user-secrets are the owner's machine config, not repo state (care item 5); not edited. Verification instead runs `$env:Database__ApplyMigrationsOnStartup='false'; dotnet test` in the same pwsh session - env vars load after user secrets, and ApiHostFactory only appends in-memory settings, so the override wins without changing any file.
- Follow-up: integration host inheriting machine user-secrets is broken behaviour (tests fail on any dev box with that secret), so it qualifies for a ticket: ApiHostFactory pins `Database:ApplyMigrationsOnStartup=false` in HostSettings. Filed via Rigger.
