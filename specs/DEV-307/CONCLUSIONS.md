# DEV-307 grill conclusions

## Authority and evidence

Phase 2 Conductor instruction for this round: every charter 2.3 item the ticket does not decide is `blocked: structural`; this overrides PRODUCT section 5 and the seat default that ordinarily delegate those decisions to Patron. These are owner checkboxes for the spec PR; none is an approved implementation choice.

Evidence: live `scripts/get-task.ps1 -TaskId DEV-307` read on 2026-10-02; `DEV-307` canvas note lines 12-22 (Wisp recon summary); `specs/PRODUCT.md`; `.specify/memory/constitution.md`. The separate `recon-DEV-307` note is not connected to Patron; its detailed receipt is unverified here. No code or verification gates were rerun.

---

## Q1 - Exact dependency set

Question (Conductor): which Serilog sinks, OTLP exporter/instrumentation, PostgreSQL health-check package and endpoint-test package should be added?

Recommendation (Keel, DEV-307-keel-q1-answer.md): Serilog.AspNetCore; OpenTelemetry.Extensions.Hosting; OpenTelemetry.Exporter.OpenTelemetryProtocol; OpenTelemetry.Instrumentation.AspNetCore; OpenTelemetry.Instrumentation.Http; Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore in Infrastructure; Microsoft.AspNetCore.Mvc.Testing for tests. Use CPM. Omit a second Serilog OTLP sink and Seq. Instrumentation compatibility with existing OpenTelemetry 1.19.1 needs recon. Cost: six proposed production package pins and one test pin.

Patron verdict: `blocked: structural - Approve the exact CPM package set and compatible pins for console JSON, OTLP traces/metrics/logs, ASP.NET Core, HttpClient, Npgsql and RabbitMQ instrumentation, PostgreSQL readiness and WebApplicationFactory tests?`

- Basis: Conductor Phase 2 instruction; PRODUCT section 5 care item 1. The ticket names technologies and required instrumentation, not the complete package IDs or versions.
- Ticket Scope & Technical Design 2 explicitly names Npgsql and RabbitMQ instrumentation: Keel's recommendation may not drop them as follow-up work. PostgreSQL readiness is expressly in Scope 3, so it is in scope. Package count and transitive package claims are provisional, not independently verified.
- Implication: the recommended package list is an incomplete proposal pending Npgsql/RabbitMQ coverage and compatibility evidence; no package or pin is approved by this entry. ServiceDefaults remains application-independent (constitution I, line 109); a DbContext-specific readiness check belongs in Infrastructure if approved.

---

## Q2 - Health response contract

Question (Conductor/Patron): what body should `/health/live` and `/health/ready` return, and does Degraded map to 200 or 503?

Recommendation (Keel, brief.md Q2): live 200 JSON `{status}`; ready 200 JSON `{status, checks:[{name,status,durationMs}]}`; Unhealthy 503 ProblemDetails containing `traceId` and the `checks` extension, with no exception text; Degraded 200. Rationale: a degraded instance may still serve traffic. Cost: a custom response writer and endpoint integration coverage. Keel also proposed excluding health routes from OpenAPI and treating check names, durationMs and status spelling as taste.

Patron verdict: `blocked: structural - Approve the complete health success/error body contract and content types, including checks and durationMs, and choose Degraded -> 200 or 503 for /health/ready?`

- Basis: Conductor Phase 2 instruction; PRODUCT section 5 care item 4. Ticket Scope 3 and ACs decide the routes, live 200 and ready 200/503, but not their body fields or the Degraded mapping.
- Constitution V (lines 195-212) requires ProblemDetails with traceId for errors; PR quality gates (lines 359-360) require new endpoints in committed OpenAPI. Reject the proposed OpenAPI exclusion: document both routes; no constitution departure is needed. No raw exception text, connection strings or check data belong in the response (constitution VII, lines 243-249).
- Implication: Keel's JSON/ProblemDetails and Degraded 200 proposal remains a recommendation pending the owner checkbox. Exposed check names, field spelling, numeric units and status casing belong to that blocked contract, not taste. Microsoft Learn confirms plaintext bodies and Degraded 200 are middleware defaults, not owner decisions: https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0#health-check-options.

---

## Decided scope and closing terms

Patron verdict: retain every live ticket requirement; close the grill after two questions with Q1 and Q2 open as structural owner checkboxes.

- Ticket Scope & Technical Design 1-4: console JSON plus OTLP logs; OTLP traces/metrics with the named endpoint/environment override; ASP.NET Core, HttpClient, Npgsql and RabbitMQ instrumentation; ActivitySource LamuFlix; live/basic ping and PostgreSQL/RabbitMQ readiness; singleton TimeProvider.System. The ticket title and constitution VI-VII retain startup options validation. No schema, LocalPlay or process-launch change is required.
- Ticket Decision dated 2026-09-28 and Acceptance addition explicitly include TracingDecorator.cs: ValidationException keeps status Unset and sets `lamuflix.handler.outcome=validation_failed`; every other exception sets Error and error.type; tests prove both. This is decided ticket scope, not an owner checkbox. Existing validation is preserved, not treated as new unimplemented scope merely because it appears in the summary.
- Standing closing bar: Critical/High findings with a concrete failure scenario; two review rounds, at most two fix commits per round (task-pipeline section 2.2; agent-pipeline Loop Discipline). Frozen scope is all the ticket items above, their host wiring, dependency registrations, required contracts and verification; anything else is a follow-up issue, not a finding in this round. A deferred Critical/High finding still leaves NEEDS FIXES. Keel records these terms in brief.md.

## Recon and handoff limits

Before drafting the implementation plan, the Conductor routes Keel's five needs-recon items: compatible package pins; RabbitMQ.Client version and ActivitySource names; TracingDecorator current behaviour; metadata-check status/tags; and ProblemDetails service availability. The package/transitive claims and exporter-provider semantics have not been independently verified by Patron. Exporter registration alone is not evidence that telemetry exports via OTLP. Patron does not run those checks or write brief.md.

Gate 1 remains closed pending the owner answers and the subsequent clean analysis/plan-challenge requirements. This grill close does not grant implementation authorization. No project-specific glossary term or irreversible architectural decision arose; no CONTEXT or ADR edit is needed.

## Decision A - Metadata-provider readiness membership

Patron verdict: untag `MetadataProviderHealthCheck` from `ready`; keep it registered and available to unfiltered health checks. This conforms to decided ticket scope; no new owner checkbox.

- Basis: live DEV-307 Scope & Technical Design 3 names PostgreSQL connection and RabbitMQ channel for readiness; `brief.md:14` (AC2) and `brief.md:26` (frozen scope 3) express that same boundary. Constitution VI (`.specify/memory/constitution.md:232-234`) names Postgres/RabbitMQ for readiness and separately requires external dependency health-check registration. Removing only the tag satisfies both requirements.
- Trusted recon: DEV-307 canvas note lines 28-31 (`recon-307-4-metacheck`) confirms both metadata-provider and RabbitMQ are currently tagged ready. Including metadata-provider would let a bad/missing OMDb key alone return 503 with healthy PostgreSQL/RabbitMQ. Under the proposed Q2c mapping an OMDb outage would instead return 200 Degraded; neither outcome belongs to the ticket's readiness dependency set.
- Infrastructure DI is already listed in `brief.md:86`; this registration adjustment is forced by Scope 3/AC2, not an unrelated file rewrite. This ruling does not approve Q2c or close the existing Q1/Q2 owner checkboxes.

## Decision B - Exception error.type spelling

Patron verdict: [assumed] use `exception.GetType().FullName` for `error.type` on non-validation exceptions in `TracingDecorator`; record this taste choice in ASSUMPTIONS.md.

- Basis: DEV-307 Decision (2026-09-28) and Acceptance addition already require every non-validation exception to set Error and `error.type`, without prescribing its spelling. PRODUCT section 4 permits a logged taste assumption; ValidationException retains the ticket's Unset/outcome behaviour.
- OpenTelemetry's current [error.type registry](https://github.com/open-telemetry/semantic-conventions/blob/main/model/error/registry.yaml), queried via Context7 on 2026-10-02, recommends the canonical class name when the value identifies an exception type, with predictable low cardinality. FQN is the selected .NET representation and distinguishes equally named exception classes in different namespaces.
- Trusted recon: DEV-307 canvas note line 28 (`recon-307-3-decorator`) reports the existing short `GetType().Name`. Constitution VI (`.specify/memory/constitution.md:225-229`) retains the existing TelemetryConstants attribute key; this ruling changes its value spelling only.
