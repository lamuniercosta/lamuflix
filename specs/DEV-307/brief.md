# DEV-307 brief: ServiceDefaults (Serilog, OpenTelemetry OTLP, health checks, TimeProvider)

Size: M. Parent: DEV-284. ui: no. Branch: `feature/307-spec`.
Author: Keel. Patron owns `CONCLUSIONS.md` and `ASSUMPTIONS.md`. This brief owns the plan decisions.
Evidence: live ticket text as Patron relayed it (DEV-307-patron-q1-q2.md, from `scripts/get-task.ps1 -TaskId DEV-307`, 2026-10-02); the `DEV-307` canvas note recon summary (lines 12-22); the constitution (Observability, error handling, stack table).

Gate 1 stays closed until the open `blocked: structural` questions are answered: **Q1 and Q2**, the two `blocked: structural` checkboxes below, **and needs N1 to N4** recorded in `plan.md` §8. Q1 and Q2 are the **user's**; so is N2's second half, the constitution departure under charter 2.3b. **N2's first half is Patron's** charter 2.3(6) ruling, because it is a change to a project file the ticket does not name. **N1 and N3 feed the two owner checkboxes rather than standing alone** - N1 needs a new package and a new top-level folder (charter 2.3 items 1 and 2), and N3 decides whether a pin has a verified version at all - and **N4 decides what Q2 is allowed to ask**, so it is answered with Q2 rather than separately. The Conductor's Phase 2 instruction says every charter 2.3 item the ticket does not decide goes to the owner this round.

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

**Need N3 is carried by this checkbox and is no longer a question of its own.** Recon round 2 pinned rows 2 to 6 and never covered rows 1, 8 and 9, so **the three unverified pins take these versions**: `Serilog.AspNetCore` **10.0.0**, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` **10.0.0** and `Microsoft.AspNetCore.Mvc.Testing` **10.0.0** (recon R4), each matching the repository's `.NET 10.0.x` line rather than moving any existing pin. **Answering Q1 answers N3**, so the checkbox below carries the rule and the owner is never asked the same question twice. `brief.md:7` is unchanged and already records that N3 feeds this checkbox rather than standing alone.

### Q2: health body shape and Degraded semantics (blocked: structural, owner checkbox)

The ASP.NET Core defaults (a plaintext body, Degraded mapped to 200) do not settle our contract. Constitution V requires every error response to be a `ProblemDetails` with `traceId`, so a plaintext 503 would be an undeclared exception to that rule. I recommend against it.

- **Q2a, success body.** Both routes return `200 application/json`.
  - `/health/live`: `{"status":"Healthy"}`.
  - `/health/ready`: `{"status":"Healthy"|"Degraded","checks":[{"name":"postgres","status":"Healthy","durationMs":12}]}`.
- **Q2b, failure body.** `/health/ready` returns 503 `application/problem+json`, written through `IProblemDetailsService` so that `traceId` is added the same way as on every other error. The body is `title: "Service Unavailable"`, `status: 503`, `traceId`, and the extension `checks` (same shape as Q2a).
  - Exception messages, check descriptions and `data` are never written. A connection failure must not leak a host or connection string (scrub list).
- **Q2c, Degraded maps to 200.** Readiness gates traffic, and a Degraded instance can still serve. Mapping Degraded to 503 would pull a partially working instance out of rotation and cause flapping. Only Unhealthy maps to 503. This meets the "200 or 503" in AC2.
  - ~~The health routes stay out of `openapi.json`.~~ **Rejected by Patron** (CONCLUSIONS Q2, constitution PR quality gates lines 359-360): both routes are documented in the committed `openapi.json`. **The one constitution departure is the deferral of that documentation, and it is the 2.3b clause appended to the Q2 checkbox below** (this file, §Q2: FR-034 rides **DEV-20** with **DEV-320** after it) — nothing else in Q2 departs from anything.
- **Cost.** One custom response writer in ServiceDefaults (about 30 lines, held under the complexity gate) plus two WebApplicationFactory tests per route state. No new packages beyond Q1.
- **Proposed checkbox:** `blocked: structural - DEV-307 Q2: health routes return JSON {status, checks[name,status,durationMs]}; /health/ready Unhealthy -> 503 ProblemDetails with traceId and checks extension (no exception text); Degraded -> 200; FR-034 (both health routes in the committed web/src/api/openapi.json, with the generated TS) is deferred to the Epic 6 web scaffold ticket DEV-20, which owns the C# to OpenAPI to TypeScript chain for both routes, with DEV-320 (generate the TypeScript API client from the committed openapi.json and configure the MSW handlers) owning the generated TS client afterwards - a 2.3b departure from constitution.md:359-360 for this ticket only?` (Patron rejected the exclusion — `CONCLUSIONS.md` Q2, `constitution.md:359-360` — so the checkbox asks about the documentation rather than proposing to skip it. The document itself does not exist yet, which is need N1; the deferral clause is how N1 is answered, so the clause and the question are the same answer.)

**Ticking the checkbox above without its deferral clause is not an answer, and this says why rather than assuming it.** Without that clause Q2 can be ticked while FR-034 stays unsatisfiable and the C# to OpenAPI to TypeScript chain for the two health routes has **no owner at all** — which is need N1 recorded as a gap and nothing else. With it, the owner either accepts the deferral, and FR-034 rides DEV-20 with DEV-320 after it, or **declines it, and this ticket is then `blocked: structural` with Gate 1 staying closed**: building the OpenAPI document here needs a new package and a new top-level folder (`PRODUCT.md:43-44`), which is precisely what the deferral exists to avoid, and this brief cannot answer that on the owner's behalf. Either answer is deliverable; an answer that leaves FR-034 orphaned is not.

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
  - **No file under `web/` is edited by this ticket** — no `openapi.json`, no `AddOpenApi`, no `types.ts`; FR-034 rides DEV-20 and DEV-320, and `tasks.md` T048 records that deferral.
  - The test projects.
  - **Added by the plan pass and recorded here so this list is the whole list (D3):**
    - `src/LamuFlix.Core/Pipeline/HealthCheckTags.cs` (**new**) — `public const string Ready = "ready"`, because the readiness predicate lives in `ServiceDefaults`, which cannot reference `Infrastructure`, so three registration sites and one predicate have to share the string through `Core`.
    - `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqServiceCollectionExtensions.cs` (**edit**) — the `["ready"]` literal at line 38 becomes `[HealthCheckTags.Ready]`. No behaviour change; named by path here because "Infrastructure DI registration" above does not name it.
    - `tests/LamuFlix.Test/TracingDecoratorTests.cs:65` (**edit**, one assertion) — forced by Decision B, which makes the short-name assertion false the moment AC4 lands, so the suite cannot be green without it. TEST-WRONG, never weakened.
    - `tests/LamuFlix.UnitTests/HealthCheckRegistrationTests.cs` (**new**) — the only test that reads the *real* registrations: `postgres` and `rabbitmq` carry the readiness tag, `metadata-provider` is registered and untagged (SC-008, FR-013). The endpoint tests replace every check with a stub, so without this file Decision A has no evidence at all.
  - Counted: **twenty-two files** under the default `WebApplicationFactory<Program>` harness D1 names — nine new and thirteen edited, the twenty-first is `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` and the twenty-second is `tests/LamuFlix.IntegrationTests/MetadataProviderProbe.cs` (an **edit** to the existing fixture, Patron Decision C) — and the eighth pin is `Microsoft.AspNetCore.Mvc.Testing`; or **twenty-one** files, nine new and twelve edited, and seven pins, that `.csproj` left unedited, if the owner approves the Kestrel-in-test departure in N2.
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

## Plan decisions recorded after the review round (D1-D4)

Recorded 2026-10-02 by Quill, fix round 1, on the Conductor's instruction adopting the recommendations the review round made. This section is the brief's own record of plan decisions, which is what `brief.md:4` reserves it for. **None of the four was an owner answer when it was recorded**, and none opened or closed a gate: D1 rides the N2 checkbox, and D4 has since been **ratified by Patron in `CONCLUSIONS.md` §Decision D4** (fix round 3), which promotes it from a plan decision to a ruling without changing what it says.

- **D1 — the test harness stays `WebApplicationFactory`, as this brief already says, and that is the plan's default.** `WebApplicationFactory<Program>` needs one thing the repository does not have, and it lives in one file: `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` must carry a `ProjectReference` to `LamuFlix.Api` and the `Microsoft.AspNetCore.Mvc.Testing` reference. Those two entries are the **first half** of need N2 and are charter 2.3(6) — Patron's ruling, not the writer's. **No entry-point line is part of that half**: on .NET 10 a source generator emits the `public partial class Program` for a top-level-statement project, so `Api/Program.cs` needs no such declaration for a test to name `Program`, and analyzer ASP0027 flags an explicit one as unnecessary — recon round 8 (`recon-307-8-program-gen.md`), verified against `global.json` pinning SDK 10.0.400 and `net10.0`. The alternative the plan earlier proposed, a real Kestrel host the test composes itself from `AddServiceDefaults()`, **is not a default and is not taken**; it is the **second half** of N2 and, because it departs from `brief.md:103`, `constitution.md:288` and `constitution.md:359-360`, it is a constitution departure and therefore the **user's** checkbox under charter 2.3b. **N2 stays one combined checkbox with both halves, and neither half is dropped**: under the default the harness boots `Program.cs` itself and the counted list is twenty-two files and eight pins, and Kestrel runs only if the owner approves the departure, which takes it back to twenty-one and seven. Constitution IX is therefore recorded as **conditional**, not as a pass, until N2 is answered.
- **D2 — the task order is this brief's order.** `brief.md` §Plan decisions, task order 1 to 7, is authoritative for the sequence, and the phases in `tasks.md` now follow it one for one: pins, clock and Serilog, OpenTelemetry, health registration and the `ready` tags, endpoint mapping and the writer, TracingDecorator, tests per step and gates last. The earlier draft ran the ungated half first (TracingDecorator and liveness before the pins), which was defensible but is not this brief's sequence. **The consequence is recorded, not hidden: the first implementation phase is now the Q1-blocked pin phase, so AC1 and AC4 are no longer deliverable while the owner's desk is empty.** Gate 1 is closed for Q1 and Q2 in any case, so nothing the pipeline could have delivered is lost — but the "mergeable increment before the owner answers" claim is gone, and no document claims it.
- **D3 — the files outside the original list are accepted and now listed above.** `HealthCheckTags.cs` is a code-shape branch with its basis stated in `plan.md` §9, not a ruling; the `RabbitMqServiceCollectionExtensions.cs` edit and the single `tests/LamuFlix.Test/TracingDecoratorTests.cs:65` assertion are forced by the readiness predicate and by Decision B respectively. Each is named in the files list with its reason, so a reviewer auditing the list reads them as decisions rather than as scope creep.
- **D4 — the meter identity is `TelemetryConstants.ActivitySourceName`, as a plan decision. RATIFIED.** `TelemetryConstants` holds exactly one identity constant and the repository holds no `Meter` at all, so a second `MeterName` constant would be a second identity string for the same application. The plan pass also wrote a matching `[assumed]` entry into `ASSUMPTIONS.md`; **Patron has ratified the decision in `CONCLUSIONS.md` §Decision D4 and the `[assumed]` tag stands** (2026-10-02). The question this entry used to leave open is closed: the meter identity is a ruling, and nothing downstream of it is an assumption.

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

- 2026-10-02 (Quill, fix round 6): on the Conductor's instruction applying Keel's final-verify. **Counts and one missing file, and nothing else — no owner checkbox is touched and no assumption is written.** Patron Decision C (`CONCLUSIONS.md` §Decision C, commit `f8c9a47`) added `tests/LamuFlix.IntegrationTests/MetadataProviderProbe.cs` as a **twenty-second file, an edit to the existing fixture**, and the count had not followed it: the default is now **twenty-two files, nine new and thirteen edited**, and **twenty-one, nine new and twelve edited**, under the Kestrel-in-test departure. The same file is added to `plan.md` §11's close-out `$all` array (the array is **eighteen** changed `.cs` files, where `tasks.md` T043 said seventeen) and to T046's named list, and the fixture edit is written into **T033A as an explicit first step** citing Decision C rather than being left as an assumption that somebody else had already made it. Every other number in the set was already correct and was left alone.
- 2026-10-02 (Quill, fix round 5): on the Conductor's instruction applying the six residual items of Keel's re-verify (`adjudication-DEV-307-plan` verification result). **This file's counts and the OpenAPI line, and nothing else in the owner checkboxes.** The counted list is now **twenty-two** files under the default harness and **twenty-one** without the `.csproj`, because T031's `tests/LamuFlix.UnitTests/OtlpEndpointEnvironmentTests.cs`, T033A's `tests/LamuFlix.IntegrationTests/MetadataProviderTelemetryTests.cs` and Decision C's `tests/LamuFlix.IntegrationTests/MetadataProviderProbe.cs` are three files the earlier count predated; the same numbers now stand in `plan.md` §Project Structure, §Technical Context, §8 N2 and §11, in `tasks.md` T001, T020A, the Phase 6 header, T043 and T046, and in D1 below. The **files-touched list no longer names `web/src/api/openapi.json`** — it says in as many words that this ticket edits no file under `web/`, which is what the FR-034 deferral and T048 mean, and the Q2 checkbox no longer opens by asserting both routes are documented in a committed document that does not exist. The struck Q2c line now points at the 2.3b deferral clause instead of claiming no constitution departure is needed, so the struck line and the live clause agree. **Q1 and Q2 themselves are unchanged in substance: no owner answer was pre-empted, and no assumption was written.** `CONCLUSIONS.md` is Patron's and its Q2 verdict is left exactly as Patron wrote it, to be recorded once the user answers.
- 2026-10-02 (Quill, fix round 4): on the Conductor's instruction applying the six items of Keel's adjudication (`adjudication-DEV-307-plan`) and the recon answers (`recon-307-9-instrumentation`). **Two checkbox texts changed and nothing else in this file.** **Q1** now carries need N3 verbatim — the three unverified pins take 10.0.0, 10.0.0 and 10.0.0 (recon R4) on the repository's `.NET 10.0.x` line — so answering Q1 answers N3 and no second box exists for the owner. **Q2** now carries a deferral clause for FR-034: the committed `openapi.json` work rides **DEV-20**, the Epic 6 web scaffold ticket that owns the C# to OpenAPI to TypeScript chain, with **DEV-320** owning the generated TS client afterwards; the clause is named as a 2.3b departure from `constitution.md:359-360` **for this ticket only**, and the paragraph under it states that ticking Q2 without the clause is not an answer and that declining the deferral leaves the ticket `blocked: structural` with Gate 1 closed. **`brief.md:7` is unchanged** — it already records that N1 and N3 feed the two owner checkboxes rather than standing alone, and both are now true of the texts above. No owner answer was pre-empted and no assumption was written.

- 2026-10-02 (Quill, fix round 1): recorded D1 to D4 above, added the four files the plan touches that this list did not name (D3), and corrected the Q2 checkbox text, which still proposed the OpenAPI exclusion Patron rejected at `CONCLUSIONS.md` Q2. No owner answer was pre-empted and no assumption was written.
- 2026-10-02 (Keel): corrected needs-recon item 4. The original wording said `MetadataProviderHealthCheck` was not tagged `ready`; it is. Folded in the recon round 2 facts and Decisions A and B. Synced Q2c, the out-of-scope list and the files touched with Patron's Q2 rejection of the OpenAPI exclusion. Q1 and Q2 owner checkboxes are unchanged.
- 2026-10-02 (Quill, fix round 3): on the Conductor's ruling adopting recon round 8 (`recon-307-8-program-gen.md`), **the `public partial class Program;` line is dropped from D1 and from `plan.md` §8 N2's first half everywhere it appeared** — .NET 10 source-generates that declaration for a top-level-statement project, so it is not needed, and ASP0027 would flag an explicit one. **N2's first half is therefore the `ProjectReference` to `LamuFlix.Api` and the `Microsoft.AspNetCore.Mvc.Testing` reference in `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj`, and nothing else**; the Kestrel-in-test half is unchanged as the 2.3b constitution departure, and the counted lists above (nineteen files and eight pins, or eighteen and seven) are unaffected. The same pass split that half out of `tasks.md`'s T020 into a new **T020A**, so the Infrastructure health package stays a Q1-only task and the harness references have their own task with their own owner answer. Also corrected here: the Gate 1 line now names needs N1 to N4 alongside Q1 and Q2, D3's "respectively" now matches the reasons it pairs (the readiness predicate forces the RabbitMQ edit, Decision B forces the `TracingDecoratorTests.cs:65` assertion), and **D4 is marked ratified** per `CONCLUSIONS.md` §Decision D4. Nothing that needs an owner answer was pre-empted.
- 2026-10-02 (Quill, fix round 2): on the Conductor's ruling, N2 stays **one combined owner checkbox with both halves**, and D1 is now the default the plan follows rather than a preference it records — `WebApplicationFactory<Program>` first, Kestrel-in-test only if the owner approves the constitution departure — so the counted list above is nineteen files and eight pins under the default and eighteen and seven under the departure. `plan.md` §8 N2 is the single source of the N2 wording; `DRAFTING_RECEIPT.md` §Escalations item 3 and `checklists/requirements.md` Notes item 3 point there instead of restating it, and the "fold both into Q1" and "defer the endpoint tests" options are gone. Every `brief.md` line citation in `plan.md`, `tasks.md`, `DRAFTING_RECEIPT.md` and `checklists/` was brought back into line with this file at that point in time. Nothing that needs an owner answer was pre-empted, and this is a note under a decision already in force, not a new decision.
