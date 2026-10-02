# DEV-307 brief: ServiceDefaults (Serilog, OpenTelemetry OTLP, health checks, TimeProvider)

Size: M. Parent: DEV-284. ui: no. Branch: `feature/307-spec`.
Author: Keel. Patron owns `CONCLUSIONS.md` and `ASSUMPTIONS.md`. This brief owns the plan decisions.
Evidence: live ticket text as Patron relayed it (DEV-307-patron-q1-q2.md, from `scripts/get-task.ps1 -TaskId DEV-307`, 2026-10-02); the `DEV-307` canvas note recon summary (lines 12-22); the constitution (Observability, error handling, stack table).

Gate 1 stays closed until the user answers the two `blocked: structural` checkboxes below (Q1 and Q2). The Conductor's Phase 2 instruction says every charter 2.3 item the ticket does not decide goes to the owner this round.

## Closing bar

The ticket's ACs:

- **AC1:** `GET /health/live` returns 200.
- **AC2:** `GET /health/ready` returns 200 or 503, according to PostgreSQL connection and RabbitMQ channel health.
- **AC3:** OTLP export of traces, metrics and logs.
- **AC4 (Scope, TracingDecorator):** a `ValidationException` leaves the span status Unset and sets `lamuflix.handler.outcome=validation_failed`. Every other exception sets status Error and `error.type`. A test proves both paths.

Review bar (standing): only Critical or High findings with a concrete failure scenario at `file:line` block the work. Anything below that bar goes in Follow-up.

## Frozen scope (every live ticket item)

1. **Logging (Scope 1):** Serilog wired once in ServiceDefaults, writing structured JSON to the console. Logs reach OTLP through the OpenTelemetry logging provider (`writeToProviders: true`). No second Serilog OTLP sink and no Seq sink.
2. **Telemetry (Scope 2):** OpenTelemetry tracing and metrics export over OTLP to `localhost:4317` by default. `OTEL_EXPORTER_OTLP_ENDPOINT` overrides the default (standard SDK env var, no custom config key). The instrumentation covers ASP.NET Core, HttpClient, Npgsql and RabbitMQ, plus `ActivitySource` "LamuFlix" and its meter. The name comes from `TelemetryConstants`, never a string literal.
3. **Health (Scope 3):** `MapHealthChecks` registered once in ServiceDefaults (the hosts do not duplicate it).
   - `/health/live`: predicate excludes every check (basic ping).
   - `/health/ready`: runs only the checks tagged `ready`, which are PostgreSQL connection and RabbitMQ channel. `MetadataProviderHealthCheck` loses its `ready` tag but stays registered (Decision A).
   - Both routes are anonymous.
   - The PostgreSQL check is registered in Infrastructure, not ServiceDefaults, because ServiceDefaults stays application-independent (constitution I).
4. **Time (Scope 4):** `TimeProvider.System` registered as a singleton in ServiceDefaults.
5. **TracingDecorator.cs (ticket-named):** the span outcome semantics in AC4, with a test for each path. `error.type` = `exception.GetType().FullName` (Decision B).
6. **Single caller:** `Api/Program.cs:11` keeps calling `AddServiceDefaults`, and adds a `MapDefaultEndpoints` call if the plan puts endpoint mapping there.

Out of scope (Follow-up candidates, Patron decides whether to file them):
- A health signal for the worker in compose. The constitution requires one, but the ticket does not list it.
- Seq.
- Runtime and EF Core instrumentation.

## Round cap

Review: 2 rounds at most (standing section 2.2). Remediation: at most 2 fix commits per round. The grill closed after 2 questions, within the cap.

## Grill answers

### Q1: exact package set (blocked: structural, owner checkbox)

Patron's verdict is in `CONCLUSIONS.md` Q1. My revised proposal, which adds the Npgsql and RabbitMQ instrumentation the ticket names in Scope 2:

| # | Package | Project | Purpose |
|---|---|---|---|
| 1 | Serilog.AspNetCore | ServiceDefaults | Console JSON (Scope 1) |
| 2 | OpenTelemetry.Extensions.Hosting | ServiceDefaults | AddOpenTelemetry for traces, metrics and logs |
| 3 | OpenTelemetry.Exporter.OpenTelemetryProtocol | ServiceDefaults | OTLP (Scope 2, AC3) |
| 4 | OpenTelemetry.Instrumentation.AspNetCore | ServiceDefaults | ASP.NET Core instrumentation |
| 5 | OpenTelemetry.Instrumentation.Http | ServiceDefaults | HttpClient instrumentation |
| 6 | Npgsql.OpenTelemetry | ServiceDefaults or Infrastructure | Npgsql instrumentation (`AddNpgsql()` on the tracer) |
| 7 | ~~RabbitMQ instrumentation package~~ | n/a | **Recon:** RabbitMQ.Client 7.2.2 traces natively. No package; `AddSource("RabbitMQ.Client.Publisher", "RabbitMQ.Client.Subscriber")`. |
| 8 | Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | Infrastructure | PostgreSQL readiness (`AddDbContextCheck`, tag `ready`) |
| 9 | Microsoft.AspNetCore.Mvc.Testing | tests | WebApplicationFactory tests of both health routes |

All of them get CPM pins. No pin is approved until recon confirms that every version is compatible with the existing OpenTelemetry 1.19.1 pin, and with the referenced Npgsql and RabbitMQ.Client versions.

### Q2: health body shape and Degraded semantics (blocked: structural, owner checkbox)

The ASP.NET Core defaults (a plaintext body, Degraded mapped to 200) do not settle our contract. Constitution V requires every error response to be a `ProblemDetails` with `traceId`, so a plaintext 503 would be an undeclared exception to that rule. I recommend against it.

- **Q2a, success body.** Both routes return `200 application/json`.
  - `/health/live`: `{"status":"Healthy"}`.
  - `/health/ready`: `{"status":"Healthy"|"Degraded","checks":[{"name":"postgres","status":"Healthy","durationMs":12}]}`.
- **Q2b, failure body.** `/health/ready` returns 503 `application/problem+json`, written through `IProblemDetailsService` so that `traceId` is added the same way as on every other error. The body is `title: "Service Unavailable"`, `status: 503`, `traceId`, and the extension `checks` (same shape as Q2a).
  - Exception messages, check descriptions and `data` are never written. A connection failure must not leak a host or connection string (scrub list).
- **Q2c, Degraded maps to 200.** Readiness gates traffic, and a Degraded instance can still serve. Mapping Degraded to 503 would pull a partially working instance out of rotation and cause flapping. Only Unhealthy maps to 503. This meets the "200 or 503" in AC2.
  - ~~The health routes stay out of `openapi.json`.~~ **Rejected by Patron** (CONCLUSIONS Q2, constitution PR quality gates lines 359-360): both routes are documented in the committed `openapi.json`. No constitution departure needed.
- **Cost.** One custom response writer in ServiceDefaults (about 30 lines, held under the complexity gate) plus two WebApplicationFactory tests per route state. No new packages beyond Q1.
- **Proposed checkbox:** `blocked: structural - DEV-307 Q2: health routes return JSON {status, checks[name,status,durationMs]}; /health/ready Unhealthy -> 503 ProblemDetails with traceId and checks extension (no exception text); Degraded -> 200; routes excluded from openapi.json?`

Taste only (for Patron to log as `[assumed]`): the check names `postgres` and `rabbitmq`; the field name `durationMs` and integer milliseconds; the camelCase status strings coming from the `HealthStatus` names.

## Plan decisions

- **Approach:** one `AddServiceDefaults` (Serilog, OpenTelemetry, health-check registration, TimeProvider, plus the options validation that already exists), and one endpoint mapping extension for the health routes. The hosts only call these.
- **Files touched (recon must confirm the paths):**
  - ServiceDefaults `Extensions.cs` (lines 10-17 today), plus a health response writer next to it.
  - ServiceDefaults `.csproj`.
  - `Directory.Packages.props`.
  - Infrastructure DI registration (add the PostgreSQL check tagged `ready`; `RabbitMqHealthCheck` is already tagged `ready`; remove the `ready` tag from `MetadataProviderHealthCheck`, Decision A).
  - `Api/Program.cs`.
  - `TracingDecorator.cs`.
  - `TelemetryConstants` (add `lamuflix.handler.outcome`; recon confirmed it is missing).
  - `web/src/api/openapi.json` (document both health routes; regenerate `types.ts` only if the generator emits them).
  - The test projects.
- **Test strategy:**
  - Unit tests: TracingDecorator, both AC4 paths, using an `ActivityListener` to capture span status and tags.
  - WebApplicationFactory: live returns 200. Ready ignores an Unhealthy metadata-provider check (Decision A). Ready returns 200 when healthy, 200 when Degraded, and 503 ProblemDetails with `traceId` and no exception text when Unhealthy. Checks are replaced with stubs so the tests do not depend on containers.
  - OTLP export (AC3): assert that the providers and the exporter are registered, through the service provider. There is no live collector in CI.
- **Gate expectations:** Roslyn, cyclomatic complexity (15, then 6 for refactor) and InspectCode all clean on the changed `.cs` files. The baseline from `-All` is clean (recon).
- **Task order:**
  1. CPM pins.
  2. TimeProvider and Serilog.
  3. OpenTelemetry and its instrumentation.
  4. Health registration and the `ready` tags.
  5. Endpoint mapping and the response writer.
  6. TracingDecorator.
  7. Tests per step, gates last.

## Needs recon (via the Conductor, before Quill drafts the plan)

1. The latest stable versions of packages 2 to 6 that are compatible with OpenTelemetry 1.19.1 and the referenced Npgsql version.
2. The referenced RabbitMQ.Client version, and its built-in ActivitySource names, or the package needed for instrumentation.
3. The path of `TracingDecorator.cs`, and its current status and tag behaviour.
4. What `MetadataProviderHealthCheck` returns when it fails (Degraded or Unhealthy), and its current tags. **Answered (corrected):** it IS already tagged `ready` (and so is `RabbitMqHealthCheck`). `Rank` returns Degraded on an OMDb outage and Unhealthy on a bad or missing OMDb key. Consequence under Q2c: an OMDb outage keeps the API in rotation (200 Degraded), while a bad OMDb key pulls it out (503). See Decision A.
5. Whether the existing `ProblemDetails` / `IExceptionHandler` setup in Api is reachable from ServiceDefaults (`IProblemDetailsService`).

All five answered by recon round 2 (chain notes `recon-307-1` to `recon-307-7`, HEAD 3d4897b), summarised under "Recon round 2 facts" below.

## Recon round 2 facts (Wisp, `recon-307-1` to `recon-307-7`)

- **Q1 pins:** OpenTelemetry.Extensions.Hosting and OpenTelemetry.Exporter.OpenTelemetryProtocol 1.19.1; OpenTelemetry.Instrumentation.AspNetCore and .Http 1.19.0; Npgsql.OpenTelemetry 10.0.3. No bump of the existing OpenTelemetry or Npgsql pins.
- **Q1 row 7, RabbitMQ:** RabbitMQ.Client 7.2.2 has built-in tracing (sources `RabbitMQ.Client.Publisher` and `RabbitMQ.Client.Subscriber`). No instrumentation package: add the two `AddSource` names. `RabbitMQ.Client.OpenTelemetry` has no stable release and is not pinned.
- **TracingDecorator:** `src/LamuFlix.Infrastructure/Pipeline/TracingDecorator.cs` (43 lines). Today every exception sets status Error with `error.type = GetType().Name`; there is no `ValidationException` case and no `lamuflix.handler.outcome` tag. AC4 is new work, and `TelemetryConstants` needs the outcome constant. See Decision B.
- **Health checks:** `RabbitMqHealthCheck` and `MetadataProviderHealthCheck` are both tagged `ready`. No PostgreSQL check and no `MapHealthChecks` exist yet. See Decision A.
- **ProblemDetails:** `IProblemDetailsService` is reachable from ServiceDefaults through the shared framework (no new pin). `AddProblemDetails` is at `Api/Program.cs:16`; `traceId` comes from `DefaultProblemDetailsWriter`.

## Owner decisions after recon (Patron, `CONCLUSIONS.md` Decisions A and B, commit ebbd323)

- **Decision A, readiness membership: untag.** `MetadataProviderHealthCheck` drops the `ready` tag. It stays registered and appears in unfiltered checks. Basis: ticket Scope 3 and AC2 name only PostgreSQL and RabbitMQ; constitution VI lines 232-234. This follows the ticket, so there is **no new owner checkbox**. Effect: OMDb outages and bad OMDb keys no longer affect `/health/ready`, so the Q2c concern about the metadata provider goes away. Does not settle Q1 or Q2.
- **Decision B, `error.type` spelling: FQN** `[assumed]`. `TracingDecorator` uses `exception.GetType().FullName` for non-validation exceptions. Basis: the OTel `error.type` registry recommends the canonical class name. Logged in `ASSUMPTIONS.md`.

## Change log

- 2026-10-02 (Keel): corrected needs-recon item 4. The original wording said `MetadataProviderHealthCheck` was not tagged `ready`; it is. Folded in the recon round 2 facts and Decisions A and B. Synced Q2c, the out-of-scope list and the files touched with Patron's Q2 rejection of the OpenAPI exclusion. Q1 and Q2 owner checkboxes are unchanged.
